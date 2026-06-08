public struct BattlePresentationContinueInputState
{
    public bool RequiresReleaseBeforeClick;
}

public static class BattlePresentationContinueInputService
{
    private static int presentationInteractionLockCount;

    public static bool IsEnemyDetailInteractionBlocked => presentationInteractionLockCount > 0;

    public static BattlePresentationContinueInputState CreateState(bool isMouseHeld)
    {
        return new BattlePresentationContinueInputState
        {
            RequiresReleaseBeforeClick = isMouseHeld
        };
    }

    public static bool ShouldContinue(
        ref BattlePresentationContinueInputState state,
        bool isMouseHeld,
        bool isMousePressedThisFrame,
        bool isBlockedGameplayClick = false)
    {
        if (isBlockedGameplayClick && isMousePressedThisFrame)
        {
            state.RequiresReleaseBeforeClick = isMouseHeld;
            return false;
        }

        if (state.RequiresReleaseBeforeClick)
        {
            if (isMouseHeld)
                return false;

            state.RequiresReleaseBeforeClick = false;
            return false;
        }

        return isMousePressedThisFrame;
    }

    public static System.IDisposable BeginPresentationInteractionLock()
    {
        presentationInteractionLockCount++;
        return new PresentationInteractionLockScope();
    }

    private sealed class PresentationInteractionLockScope : System.IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            if (presentationInteractionLockCount > 0)
                presentationInteractionLockCount--;
        }
    }
}

public static class BattlePresentationRangePulseService
{
    public static float Evaluate(
        float elapsedSeconds,
        float durationSeconds,
        float frequency,
        bool continuous)
    {
        float safeDuration = durationSeconds <= 0f ? 0.01f : durationSeconds;
        float safeElapsed = elapsedSeconds < 0f ? 0f : elapsedSeconds;
        float normalized = UnityEngine.Mathf.Clamp01(safeElapsed / safeDuration);
        if (!continuous)
        {
            if (safeElapsed >= safeDuration)
                return 0f;

            float envelope = UnityEngine.Mathf.Sin(normalized * UnityEngine.Mathf.PI);
            float normalizedWave = 0.5f - 0.5f * UnityEngine.Mathf.Cos(normalized * frequency * UnityEngine.Mathf.PI * 2f);
            return UnityEngine.Mathf.Clamp01(envelope * normalizedWave);
        }

        float intro = SmoothStep01(normalized);
        float wave = 0.5f - 0.5f * UnityEngine.Mathf.Cos(safeElapsed * frequency * UnityEngine.Mathf.PI * 2f);
        return UnityEngine.Mathf.Clamp01(intro * wave);
    }

    private static float SmoothStep01(float value)
    {
        float clamped = UnityEngine.Mathf.Clamp01(value);
        return clamped * clamped * (3f - 2f * clamped);
    }

}
