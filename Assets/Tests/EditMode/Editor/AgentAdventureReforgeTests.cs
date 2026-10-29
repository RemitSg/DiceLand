using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public sealed class AgentAdventureReforgeTests
{
    private AgentAdventureSession session;
    private Random.State outside;
    private MapRunSnapshot originalGlobal;
    private readonly List<ScriptableObject> temporary = new List<ScriptableObject>();
    private AgentAdventureMapRuntime Map => (AgentAdventureMapRuntime)typeof(AgentAdventureSession).GetField("map",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(session);
    private static FieldInfo Global => typeof(MapRunRuntime).GetField("currentSnapshot",BindingFlags.Static|BindingFlags.NonPublic);
    [SetUp] public void SetUp() { outside=Random.state; originalGlobal=(MapRunSnapshot)Global.GetValue(null); session=new AgentAdventureSession(new AgentAdventureRequest { seed=1234 }); }
    [TearDown] public void TearDown() { session?.Dispose(); foreach(var v in temporary) UnityEngine.Object.DestroyImmediate(v); temporary.Clear(); Global.SetValue(null,originalGlobal); Random.state=outside; }
    private AgentAdventureRequest Command(string op,string asset=null)
    { var s=session.Observe(); return new AgentAdventureRequest {op=op,assetId=asset,sessionId=s.sessionId,revision=s.revision,requestId=Guid.NewGuid().ToString("N")}; }
    private AgentAdventureObservation Act(string op,string asset=null) => session.Act(Command(op,asset)).state;
    private static string Id(UnityEngine.Object value) => AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(value));
    private void Enter()
    {
        // Directed tests inject a rest tile/resources. Full normal-content HTTP evidence is separate.
        var tile=Map.LegalDestinations().First(); tile.tileType=MapTileType.Rest; tile.isResolved=false;
        var request=Command("run_move"); request.x=tile.coord.x; request.y=tile.coord.y; session.Act(request); Act("run_open_reforge");
    }
    private LootItem_SO Loot => Map.Configuration.reforge.LootPool.First(l=>l!=null&&l.rarity==LootRarity.Common&&l.attribute!=null&&RunRestService.ReforgeAttributes(Map.Configuration.reforge).Contains(l.attribute));
    private Relic_SO Material => Map.Configuration.reforge.RelicPool.First(r=>r!=null&&r.rarity==LootRarity.Common&&r.builtInEffects.All(e=>e==null||e.trigger!=Relic_SO.RelicTrigger.OnRelicAcquired));
    private void SetRelicResult(Relic_SO result)
    {
        var table=ScriptableObject.CreateInstance<RelicTable_SO>(); temporary.Add(table);
        typeof(RelicTable_SO).GetField("relics",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(table,new List<Relic_SO>{result});
        Map.Configuration.reforge.relicTable=table;
    }
    private void SelectLoot(LootItem_SO value)
    { if(session.Observe().reforge.mode!="loot") Act("run_reforge_loot"); Act("run_reforge_first",Id(value)); Act("run_reforge_second",Id(value)); Act("run_reforge_attribute",Id(value.attribute)); }
    private void SelectRelic(Relic_SO value)
    { if(session.Observe().reforge.mode!="relic") Act("run_reforge_relic"); Act("run_reforge_first",Id(value)); Act("run_reforge_second",Id(value)); }

    [Test] public void OpeningAndCancellingBeforeSuccessDoesNotConsumeTheRestVisit()
    {
        Enter(); var s=Act("run_reforge_cancel"); Assert.AreEqual("rest",s.phase);
        Assert.IsFalse(s.tiles.Single(t=>t.x==s.x&&t.y==s.y).resolved);
        Assert.IsTrue(s.legalActions.Any(a=>a.op=="run_rest_heal"));
    }
    [Test] public void LootPreviewAndSelectionAreReadOnlyAndFirstSuccessLocksTheVisit()
    {
        var loot=Loot; Map.Snapshot.runState.AddLoot(loot,4); Enter(); SelectLoot(loot);
        var s=session.Observe(); Assert.IsNull(s.reforge.resultPreview);
        Assert.IsTrue(s.reforge.possibleResults.Any(r=>r.id==Id(loot))); Assert.IsTrue(s.reforge.canConfirm);
        string before=JsonUtility.ToJson(Map.Snapshot), rng=JsonUtility.ToJson(Random.state), observed=JsonUtility.ToJson(s);
        for(int i=0;i<5;i++) Assert.AreEqual(observed,JsonUtility.ToJson(session.Observe()));
        Assert.AreEqual(before,JsonUtility.ToJson(Map.Snapshot)); Assert.AreEqual(rng,JsonUtility.ToJson(Random.state));
        var request=Command("run_reforge_confirm"); var result=session.Act(request);
        Assert.AreEqual("reforge",result.state.phase); Assert.AreEqual(1,result.state.reforge.completed);
        Assert.AreEqual(JsonUtility.ToJson(result),JsonUtility.ToJson(session.Act(request)));
        Assert.IsFalse(result.state.legalActions.Any(a=>a.op=="run_reforge_cancel"));
        Assert.Throws<ArgumentException>(()=>Act("run_rest_heal")); Assert.Throws<ArgumentException>(()=>Act("run_reforge_cancel"));
        s=Act("run_reforge_finish"); Assert.AreEqual("map",s.phase); Assert.IsTrue(s.tiles.Single(t=>t.x==s.x&&t.y==s.y).resolved);
    }
    [Test] public void OneMaterialAndInvalidAssetsCannotBeSelectedAsASecondStack()
    {
        var loot=Loot; Map.Snapshot.runState.AddLoot(loot,1); Enter(); Act("run_reforge_first",Id(loot));
        int revision=session.Observe().revision;
        Assert.Throws<ArgumentException>(()=>Act("run_reforge_second",Id(loot)));
        Assert.Throws<ArgumentException>(()=>Act("run_reforge_first","not-an-asset"));
        Assert.Throws<ArgumentException>(()=>Act("run_reforge_confirm")); Assert.AreEqual(revision,session.Observe().revision);
    }
    [Test] public void RelicPreviewMatchesResultAndSecondSuccessAutomaticallyConsumesTheRest()
    {
        var material=Material;
        var result=Map.Configuration.reforge.RelicPool.First(r=>r.rarity==LootRarity.Uncommon&&r.builtInEffects.All(e=>e==null||(e.trigger!=Relic_SO.RelicTrigger.OnRelicAcquired&&e.effectType!=Relic_SO.RelicBuiltInEffectType.IncreaseRestReforgeLimit)));
        SetRelicResult(result); Map.Snapshot.runState.AddRelic(material,4); Enter();
        SelectRelic(material); Assert.AreEqual(Id(result),session.Observe().reforge.resultPreview.id); Act("run_reforge_confirm");
        SelectRelic(material); var s=Act("run_reforge_confirm");
        Assert.AreEqual("map",s.phase); Assert.AreEqual(0,Map.Snapshot.runState.GetRelicAmount(material));
        Assert.AreEqual(2,Map.Snapshot.runState.GetRelicAmount(result)); Assert.AreEqual(Id(result),s.lastReforgeResult.id);
        Assert.IsFalse(s.legalActions.Any(a=>a.op=="run_interact")); Assert.Throws<ArgumentException>(()=>Act("run_reforge_confirm"));
    }
    [Test] public void RelicAcquisitionRunsOnceAndDuplicateConfirmDoesNotGrantAnotherPackage()
    {
        var material=Material;
        var package=Map.Configuration.reforge.RelicPool.First(r=>r.relicId=="92");
        SetRelicResult(package); Map.Snapshot.runState.AddRelic(material,2); Enter(); SelectRelic(material);
        var request=Command("run_reforge_confirm"); var result=session.Act(request);
        Assert.AreEqual(1,Map.Snapshot.runState.GetRelicAmount(package)); Assert.Greater(Map.Snapshot.runState.relicInventory.Count,1);
        string inventory=JsonUtility.ToJson(Map.Snapshot.runState);
        session.Act(request); Assert.AreEqual(inventory,JsonUtility.ToJson(Map.Snapshot.runState));
        Assert.AreEqual(JsonUtility.ToJson(result),JsonUtility.ToJson(session.Act(request)));
    }
    [Test] public void AdditionalReforgeRelicKeepsTheVisitOpenAfterTwoSuccessfulReforges()
    {
        var bonus=ScriptableObject.CreateInstance<Relic_SO>(); temporary.Add(bonus); bonus.rarity=LootRarity.Legendary;
        bonus.builtInEffects.Add(new Relic_SO.RelicBuiltInEffectEntry {trigger=Relic_SO.RelicTrigger.OnRestReforgeLimitCalculated,effectType=Relic_SO.RelicBuiltInEffectType.IncreaseRestReforgeLimit,amount=1});
        var loot=Loot; Map.Snapshot.runState.AddRelic(bonus); Map.Snapshot.runState.AddLoot(loot,6); Enter();
        for(int i=0;i<2;i++) {SelectLoot(loot); Act("run_reforge_confirm");}
        var s=session.Observe(); Assert.AreEqual("reforge",s.phase); Assert.AreEqual(2,s.reforge.completed); Assert.AreEqual(3,s.reforge.limit);
        Assert.IsTrue(s.legalActions.Any(a=>a.op=="run_reforge_finish"));
    }
    [Test] public void EmptyHigherTierPoolDoesNotOfferAConfirmAction()
    {
        var material=Material; SetRelicResult(null); Map.Snapshot.runState.AddRelic(material,2); Enter(); SelectRelic(material);
        var s=session.Observe(); Assert.IsNull(s.reforge.resultPreview); Assert.IsFalse(s.reforge.canConfirm);
        Assert.IsFalse(s.legalActions.Any(a=>a.op=="run_reforge_confirm"));
        Assert.AreEqual(2,Map.Snapshot.runState.GetRelicAmount(material));
    }
    [Test] public void ExplicitReforgeWriterLeavesTheUiSnapshotUntouched()
    {
        MapRunRuntime.RestoreSnapshot(new MapRunSnapshot {sceneName="UserSentinel",runState=new RunState {money=777}});
        string before=JsonUtility.ToJson(Global.GetValue(null)); bool pending=RestSceneRuntime.HasPendingVisit;
        var loot=Loot; Map.Snapshot.runState.AddLoot(loot,2); Enter(); SelectLoot(loot); Act("run_reforge_confirm"); Act("run_reforge_finish");
        Assert.AreEqual(before,JsonUtility.ToJson(Global.GetValue(null))); Assert.AreEqual(pending,RestSceneRuntime.HasPendingVisit);
    }
}
