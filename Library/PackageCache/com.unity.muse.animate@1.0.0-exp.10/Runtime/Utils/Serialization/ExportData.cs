namespace Unity.Muse.Animate.Usd
{
    class ExportData
    {
        public BakedTimelineModel BackedTimeline { get; }
        public ActorExportData[] ActorsData { get; }
        public PropExportData[] PropsData { get; }

        public ExportData(BakedTimelineModel backedTimeline, ActorExportData[] actorsData, PropExportData[] propsData)
        {
            BackedTimeline = backedTimeline;
            ActorsData = actorsData;
            PropsData = propsData;
        }
            
        public class ActorExportData
        {
            public ActorDefinitionComponent ActorDefinitionComponent { get; }
            public ArmatureMappingComponent PosingArmature { get; }
            public ActorModel ActorModel { get; }
            public string UsdID { get; }
            public string InstanceID { get; }

            public ActorExportData(ActorDefinitionComponent actorDefinitionComponent,
                ActorModel actor,
                int actorIndex,
                ArmatureMappingComponent posingArmature = null)
            {
                ActorDefinitionComponent = actorDefinitionComponent;
                ActorModel = actor;
                    
                UsdID = GetUsdID(actor.PrefabID);
                InstanceID = UsdID + actorIndex;
                PosingArmature = posingArmature;
            }
            static string GetUsdID(string prefabID)
            {
                return prefabID switch
                {
                    "biped" => "Puppet",
                    "quadruped" => "Quadruped",
                    _ => null
                };
            }
        }

        public class PropExportData
        {
            public PropModel PropModel { get; }
            public PropDefinitionComponent PropDefinitionComponent { get; }
            public int PropIndex { get; }
            
            public string UsdID { get; }
            public string InstanceID { get; }
            public PropExportData(StageModel stage, PropModel propModel, PropDefinitionComponent propDefinitionComponent, int propIndex)
            {
                PropModel = propModel;
                PropDefinitionComponent = propDefinitionComponent;
                PropIndex = propIndex;


                stage.PropRegistry.TryGetPropInfo(propModel.PrefabID, out var propDef);
                
                UsdID = propDef.Prefab.name;
                InstanceID = UsdID + propIndex;
            }
        }
    }
}
