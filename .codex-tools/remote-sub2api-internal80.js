const { Client } = require('ssh2');

const host = process.env.SUB2API_SSH_HOST;
const username = process.env.SUB2API_SSH_USER;
const password = process.env.SUB2API_SSH_PASSWORD;

if (!host || !username || !password) {
  console.error('Missing SUB2API_SSH_HOST, SUB2API_SSH_USER, or SUB2API_SSH_PASSWORD');
  process.exit(2);
}

const remoteScript = String.raw`
set -eu
cd /opt/sub2api
echo "== before =="
sudo docker ps --format '{{.Names}}\t{{.Ports}}\t{{.Status}}' | grep sub2api || true
ts="$(date +%Y%m%d-%H%M%S)"
cp docker-compose.yml "docker-compose.yml.bak-internal80-${ts}"
if [ -f .env ]; then
  cp .env ".env.bak-internal80-${ts}"
fi
python3 - <<'PY'
from pathlib import Path

p = Path("docker-compose.yml")
s = p.read_text()
s = s.replace("${BIND_HOST:-0.0.0.0}:${SERVER_PORT:-8080}:8080", "${BIND_HOST:-0.0.0.0}:${SERVER_PORT:-80}:80")
s = s.replace("${BIND_HOST:-0.0.0.0}:${SERVER_PORT:-80}:8080", "${BIND_HOST:-0.0.0.0}:${SERVER_PORT:-80}:80")
s = s.replace("- SERVER_PORT=8080", "- SERVER_PORT=80")
s = s.replace("http://localhost:8080/health", "http://localhost:80/health")
p.write_text(s)
PY
touch .env
if grep -q '^SERVER_PORT=' .env; then
  sed -i 's/^SERVER_PORT=.*/SERVER_PORT=80/' .env
else
  printf '\nSERVER_PORT=80\n' >> .env
fi
if grep -q '^BIND_HOST=' .env; then
  sed -i 's/^BIND_HOST=.*/BIND_HOST=0.0.0.0/' .env
else
  printf 'BIND_HOST=0.0.0.0\n' >> .env
fi
echo "== compose sub2api port lines =="
grep -nE 'SERVER_PORT|localhost:80|:80|8080' docker-compose.yml | head -30 || true
echo "== restart =="
sudo docker compose up -d --force-recreate sub2api
sleep 8
echo "== after =="
sudo docker ps --format '{{.Names}}\t{{.Ports}}\t{{.Status}}' | grep sub2api || true
echo "== health =="
curl -fsS -m 10 http://127.0.0.1/health
echo
`;

function execScript(conn, script) {
  return new Promise((resolve, reject) => {
    conn.exec('bash -s', { pty: false }, (err, stream) => {
      if (err) return reject(err);
      let stdout = '';
      let stderr = '';
      stream.on('close', (code) => resolve({ code, stdout, stderr }));
      stream.on('data', (data) => { stdout += data.toString(); });
      stream.stderr.on('data', (data) => { stderr += data.toString(); });
      stream.end(script);
    });
  });
}

async function main() {
  const conn = new Client();
  await new Promise((resolve, reject) => {
    conn.on('ready', resolve);
    conn.on('keyboard-interactive', (_name, _instructions, _lang, prompts, finish) => {
      finish(prompts.map(() => password));
    });
    conn.on('error', reject);
    conn.connect({
      host,
      port: 22,
      username,
      password,
      readyTimeout: 15000,
      tryKeyboard: true,
    });
  });

  try {
    const result = await execScript(conn, remoteScript);
    process.stdout.write(result.stdout);
    if (result.stderr) process.stderr.write(result.stderr);
    process.exitCode = result.code;
  } finally {
    conn.end();
  }
}

main().catch((err) => {
  console.error(err && err.stack ? err.stack : String(err));
  process.exit(1);
});
