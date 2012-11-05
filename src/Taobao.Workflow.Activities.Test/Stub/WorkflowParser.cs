using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Taobao.Activities;
using Taobao.Activities.Statements;
using Taobao.Workflow.Activities.Statements;

namespace Taobao.Workflow.Activities.Test.Stub
{
    //本测试工程的大部分测试均基于此解析器提供的桩工作流定义，需谨慎变更 禁止修改任何节点设置、顺序、名称等

    /// <summary>
    /// 用于常规测试的解析器桩模块
    /// 包含对常规流程和常规子流程测试支持
    /// </summary>
    public class WorkflowParser : IWorkflowParser
    {
        public static IEnumerable<ActivitySetting> GetActivitySettings(string typeName)
        {
            return typeName.Contains(BaseTest.SubFlag) ? GetSubProcessActivitySettings() : GetActivitySettings();
        }
        public static IEnumerable<ActivitySetting> GetActivitySettings()
        {
            //0
            yield return GetFirst(null);
            //2
            yield return new HumanSetting(2
                , "节点2"
                , new string[] { "完成" }
                , 0
                , HumanSetting.SlotDistributionMode.AllAtOnce
                , ""
                , new StartRule(null, 0.1, null)
                , new HumanActionerRule("originator")
                , null
                , null
                , false);
            //4
            yield return new HumanSetting(4
                , "节点3"
                , new string[] { "完成" }
                , 2
                , HumanSetting.SlotDistributionMode.OneAtOnce
                , ""
                , null
                , new HumanActionerRule("originator", "getSuperior()", BaseTest.VariableUser)
                , new FinishRule(new Dictionary<string, string>())
                , null
                , false);
            //6
            yield return new ServerSetting(6
                , "节点4"
                , "'1'"
                , "v1"
                , null
                , null
                , false);
            //8
            yield return new ParallelSetting(8, "并行节点", null, false);
            yield return new HumanSetting(8, "并行子节点1", new string[] { "完成" }, 0
                , HumanSetting.SlotDistributionMode.AllAtOnce, ""
                , StartRule.UnlimitedDelay()//无限期延迟
                , new HumanActionerRule("originator")
                , null, null, true);
            yield return new HumanSetting(8, "并行子节点2", new string[] { "完成" }, 0
                , HumanSetting.SlotDistributionMode.AllAtOnce, ""
                , null, new HumanActionerRule("getSuperior()")
                , null, null, true);
            //9
            yield return new HumanSetting(9
                , "节点6"
                , new string[] { "同意", "否决" }
                , 2
                , HumanSetting.SlotDistributionMode.AllAtOnce
                , ""
                , null
                , new HumanActionerRule("originator")
                , new FinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" } })
                , null
                , false);
            //11
            yield return new HumanSetting(11
                , "节点7"
                , new string[] { "同意", "否决" }
                , 1
                , HumanSetting.SlotDistributionMode.AllAtOnce
                , ""
                , null
                , new HumanActionerRule("originator", "getSuperior()")
                , new FinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" } })
                , null
                , false);
        }
        public static IEnumerable<ActivitySetting> GetSubProcessActivitySettings()
        {
            var list = GetActivitySettings().ToList();
            list.Add(new SubProcessSetting(BaseTest.SubProcessNodeIndex, BaseTest.SubProcessTypeName, BaseTest.SubProcessNode, null, null, false));
            return list;
        }

        public static IEnumerable<ActivitySetting> GetEscalationActivitySettings(string typeName)
        {
            if (typeName.Contains(BaseTest.EscalationGotoFlag))
                return GetEscalationGotoActivitySettings();
            if (typeName.Contains(BaseTest.EscalationRedirectFlag))
                return GetEscalationRedirectActivitySettings();
            return GetEscalationNotifyActivitySettings();
        }
        public static IEnumerable<ActivitySetting> GetEscalationNotifyActivitySettings()
        {
            var list = GetActivitySettings().ToList();
            list.RemoveAt(0);
            list.Insert(0, GetFirst(new HumanEscalationRule(BaseTest.EscalationExpiration
                , null
                , BaseTest.EscalationExpiration
                , "TestNotifyEmailTemplateName"
                , null, null)));
            return list;
        }
        public static IEnumerable<ActivitySetting> GetEscalationRedirectActivitySettings()
        {
            var list = GetActivitySettings().ToList();
            list.RemoveAt(0);
            list.Insert(0, GetFirst(new HumanEscalationRule(BaseTest.EscalationExpiration
                , null, null, null, null
                , "'" + BaseTest.EscalationRedirectToUserName + "'")));
            return list;
        }
        public static IEnumerable<ActivitySetting> GetEscalationGotoActivitySettings()
        {
            var list = GetActivitySettings().ToList();
            list.RemoveAt(0);
            list.Insert(0, GetFirst(new HumanEscalationRule(BaseTest.EscalationExpiration
                , null, null, null
                , BaseTest.EscalationGotoActivity
                , null)));
            return list;
        }

        private static ActivitySetting GetFirst(HumanEscalationRule rule)
        {
            return new HumanSetting(0
                , "节点1"
                , new string[] { "同意", "否决" }
                , -1
                , HumanSetting.SlotDistributionMode.AllAtOnce
                , ""
                , new StartRule(null, null, null)
                , new HumanActionerRule("originator")
                , new FinishRule(new Dictionary<string, string>() { { "同意", "all('同意')" } })
                , rule
                , false);
        }

        #region IWorkflowParser Members

        public WorkflowActivity Parse(string workflowDefinition, IEnumerable<ActivitySetting> activitySettings)
        {
            return this.Parse(null, workflowDefinition, activitySettings);
        }

        public virtual Statements.WorkflowActivity Parse(string key, string workflowDefinition, IEnumerable<ActivitySetting> activitySettings)
        {
            //是否是子流程测试模式
            var isSubMode = key.Contains(BaseTest.SubFlag);
            var settings = isSubMode ? GetSubProcessActivitySettings() : GetActivitySettings();

            var flow = new Statements.WorkflowActivity();
            var v1 = new Variable<string>("v1");
            flow.Variables.Add(v1);

            //0 节点1 同意|否决
            //2 节点2 完成 startRule 10s
            //4 节点3 完成 OneAtOnce slot=2 发起人->主管->username1
            //6 节点4 Server
            //8 并行节点 子节点1完成则完成
            //8 并行子节点1 发起人 startRule 无限期延迟 需要提前唤醒
            //8 并行子节点2 主管
            //9 节点6 完成 AllAtOnce slot=2 发起人 同意|否决
            //11 节点7 完成 AllAtOnce slot=1 发起人|主管 同意|否决

            //根据参数追加一个子流程节点
            //13 节点8 子流程节点
            var eighth = isSubMode
                ? WorkflowBuilder.CreateSubProcess(settings.ElementAt(9), BaseTest.SubProcessNode, null, null, null, null)
                : null;

            var seventh = WorkflowBuilder.CreateHuman(settings.ElementAt(8)
                , "节点7"
                , new GetUsers("originator", "getSuperior()")
                , flow.CustomActivityResult
                , null
                , isSubMode ? eighth : null);
            var sixth = WorkflowBuilder.CreateHuman(settings.ElementAt(7)
                , "节点6"
                , new GetUsers("originator")
                , flow.CustomActivityResult
                , null
                , seventh);

            var parallel_1 = WorkflowBuilder.CreateHuman(settings.ElementAt(5), "并行子节点1", new GetUsers("originator"), null);
            var parallel_2 = WorkflowBuilder.CreateHuman(settings.ElementAt(6), "并行子节点2", new GetUsers("getSuperior()"), null);
            //true
            var parallel = WorkflowBuilder.CreateParallel(settings.ElementAt(4), "并行节点", "true", sixth, parallel_1, parallel_2);

            var forth = WorkflowBuilder.CreateServer(settings.ElementAt(3)
                , "节点4", "'1'", null, v1, flow.CustomActivityResult, null, parallel);
            var third = WorkflowBuilder.CreateHuman(settings.ElementAt(2)
                , "节点3"
                , new GetUsers("originator", "getSuperior()", BaseTest.VariableUser)
                , flow.CustomActivityResult
                , null
                , forth);
            var second = WorkflowBuilder.CreateHuman(settings.ElementAt(1)
                , "节点2"
                , new GetUsers("originator")
                , flow.CustomActivityResult
                , null
                , third);

            flow.Body.StartNode = WorkflowBuilder.CreateHuman(settings.ElementAt(0)
                , "节点1"
                , new GetUsers("originator")
                , flow.CustomActivityResult
                , new Dictionary<string, FlowNode>() { { "同意", second } }
                , null);

            //有需要可在此初始化元数据
            //Taobao.Activities.ActivityUtilities.InitializeActivity(flow);
            return flow;
        }

        public virtual string Parse(IEnumerable<ActivitySetting> activitySettings)
        {
            return Resource.Settings5;
        }

        public virtual ActivitySetting[] ParseActivitySettings(string workflowDefinition, string activitySettingsDefinition)
        {
            throw new NotImplementedException();
        }

        public virtual string Parse(WorkflowActivity workflowActivity, string originalWorkflowDefinition)
        {
            return Resource.WorkflowDefinition5;
        }

        public virtual string ParseWorkflowDefinition(string workflowDefinition, string activitySettingsDefinition)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}