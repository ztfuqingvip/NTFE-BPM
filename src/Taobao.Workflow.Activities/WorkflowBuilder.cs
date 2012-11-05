/*
    Copyright (C) 2012  Alibaba

	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using Taobao.Activities;
using Taobao.Activities.Statements;
using Taobao.Workflow.Activities.Statements;
using Taobao.Activities.Hosting;
using Taobao.Activities.Expressions;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 提供工作流的创建帮助
    /// </summary>
    public static class WorkflowBuilder
    {
        /// <summary>
        /// 默认的无效索引
        /// </summary>
        public static readonly int Default_FlowNodeIndex = 100000000;
        /// <summary>
        /// 内置变量名-流程图当前节点的变量名
        /// </summary>
        public static readonly string Variable_CurrentNode = "CurrentNode";
        /// <summary>
        /// 内置变量名-流程发起人
        /// </summary>
        public static readonly string Variable_Originator = "originator";

        /// <summary>
        /// 创建工作流实例
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static WorkflowInstance CreateInstance(Process p, IWorkflowParser parser)
        {
            var workflow = parser.Parse(GetCacheKey(p.ProcessType)
                , p.ProcessType.Workflow.Serialized
                , p.ProcessType.ActivitySettings);
            var instance = new WorkflowInstance(workflow, p.ID, p.GetInputs());
            //HACK:【重要】强制让Core调度使用同步上下文进行调度而不使用Threaded模式，使得事务与resumption嵌套，便于事务控制和错误处理 20120507
            instance.SynchronizationContext = new WorkflowInstance.DefaultSynchronizationContext();
            //标记为可持久化
            instance.IsPersistable = true;
            //数据字段扩展
            instance.Extensions.Add<DataFieldExtension>(WorkflowBuilder.CreateDataFieldExtension(p));
            //自定义节点扩展
            instance.Extensions.Add<CustomExtension>(new CustomExtension(p.ProcessType.ActivitySettings
                .Where(o => o is CustomSetting)
                .Select(o => o as CustomSetting)
                .ToList()));
            //人工节点扩展
            instance.Extensions.Add<HumanExtension>(new HumanExtension());
            //服务端节点扩展
            instance.Extensions.Add<ServerExtension>(new ServerExtension());
            //并行节点扩展
            instance.Extensions.Add<ParallelExtension>(new ParallelExtension());
            //子流程节点扩展
            instance.Extensions.Add<SubProcessExtension>(new SubProcessExtension());
            return instance;
        }
        /// <summary>
        /// 获取工作流定义的缓存键
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetCacheKey(ProcessType type)
        {
            return type.Name + "_" + type.ID;
        }
        /// <summary>
        /// 创建数据字段扩展
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        public static DataFieldExtension CreateDataFieldExtension(Process p)
        {
            return new DataFieldExtension(p.Originator.UserName, p.GetCurrentNode(), p.GetDataFields());
        }

        #region Human
        /// <summary>
        /// 创建人工节点
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="displayName"></param>
        /// <param name="actioner"></param>
        /// <param name="humanResultTo"></param>
        /// <param name="nexts"></param>
        /// <returns></returns>
        public static FlowStep CreateHuman(ActivitySetting setting
            , string displayName
            , Expression<Func<ActivityContext, string[]>> actioner
            , Variable<string> humanResultTo
            , IDictionary<string, FlowNode> nexts
            , FlowNode defaultFlowNode)
        {
            return null;
            //return CreateHuman(setting
            //    , displayName
            //    , new Taobao.Activities.Expressions.LambdaValue<string[]>(actioner)
            //    , humanResultTo
            //    , nexts
            //    , defaultFlowNode);
        }
        /// <summary>
        /// 创建人工节点
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="displayName"></param>
        /// <param name="actioner"></param>
        /// <param name="humanResultTo"></param>
        /// <param name="nexts"></param>
        /// <param name="defaultFlowNode"></param>
        /// <returns></returns>
        public static FlowStep CreateHuman(ActivitySetting setting
            , string displayName
            , IActionersHelper actioner//Activity<string[]> actioner
            , Variable<string> humanResultTo
            , IDictionary<string, FlowNode> nexts
            , FlowNode defaultFlowNode)
        {
            var human = CreateHuman(setting, displayName, actioner, humanResultTo);
            var step = new FlowStep();
            step.Action = human;

            if (nexts == null && defaultFlowNode == null) return step;

            //设置finish cases
            //HACK:在进入switch之前就已经计算出任务结果
            var flowSwitch = new FlowSwitch<string>(o => humanResultTo.Get(o));
            if (defaultFlowNode != null)
                flowSwitch.Default = defaultFlowNode;
            if (nexts != null)
                nexts.ToList().ForEach(o => flowSwitch.Cases.Add(o.Key, o.Value));
            step.Next = flowSwitch;
            return step;
        }
        /// <summary>
        /// 创建人工节点
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="displayName"></param>
        /// <param name="actioner"></param>
        /// <param name="humanResultTo"></param>
        /// <returns></returns>
        public static Human CreateHuman(ActivitySetting setting
            , string displayName
            , IActionersHelper actioner//Activity<string[]> actioner
            , Variable<string> humanResultTo)
        {
            //HACK:实现类似于K2的client event
            var first = setting.FlowNodeIndex == Default_FlowNodeIndex;
            var human = first
                ? new Human(actioner)
                : new Human(setting.FlowNodeIndex, actioner);
            if (first)
                human.OnFlowNodeIndex = o => setting.SetFlowNodeIndex(o);

            human.DisplayName = displayName;
            if (humanResultTo != null)
                human.Result = new OutArgument<string>(humanResultTo);
            return human;
        }
        #endregion

        #region Server
        /// <summary>
        /// 创建Server节点
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="displayName"></param>
        /// <param name="serverScript">节点执行内容脚本</param>
        /// <param name="finishRule">节点完成规则</param>
        /// <param name="serverScriptResultTo">执行内容的结果输出到指定变量</param>
        /// <param name="serverResultTo">节点执行结果输出到变量</param>
        /// <param name="nexts"></param>
        /// <param name="defaultFlowNode"></param>
        /// <returns></returns>
        public static FlowStep CreateServer(ActivitySetting setting
            , string displayName
            , string serverScript
            , IDictionary<string, string> finishRule
            , Variable<string> serverScriptResultTo
            , Variable<string> serverResultTo
            , IDictionary<string, FlowNode> nexts
            , FlowNode defaultFlowNode)
        {
            var server = CreateServer(setting, displayName, serverScript, finishRule, serverScriptResultTo, serverResultTo);
            var step = new FlowStep();
            step.Action = server;

            if (nexts == null && defaultFlowNode == null) return step;

            //设置finish cases
            var flowSwitch = new FlowSwitch<string>(o => serverResultTo.Get(o));
            if (defaultFlowNode != null)
                flowSwitch.Default = defaultFlowNode;
            if (nexts != null)
                nexts.ToList().ForEach(o => flowSwitch.Cases.Add(o.Key, o.Value));
            step.Next = flowSwitch;
            return step;
        }
        /// <summary>
        /// 创建Server节点
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="displayName"></param>
        /// <param name="serverScript"></param>
        /// <param name="finishRule"></param>
        /// <param name="serverScriptResultTo"></param>
        /// <param name="serverResultTo"></param>
        /// <returns></returns>
        public static Server CreateServer(ActivitySetting setting
            , string displayName
            , string serverScript
            , IDictionary<string, string> finishRule
            , Variable<string> serverScriptResultTo
            , Variable<string> serverResultTo)
        {
            //HACK:实现类似于K2的server event
            var first = setting.FlowNodeIndex == Default_FlowNodeIndex;
            var server = first ? new Server() : new Server(setting.FlowNodeIndex);
            if (first) server.OnFlowNodeIndex = o => setting.SetFlowNodeIndex(o);

            server.DisplayName = displayName;
            server.Script = new InArgument<string>(serverScript);
            server.FinishRule = finishRule;
            //设置脚本执行结果输出到变量
            if (serverScriptResultTo != null)
                server.SetScriptResultTo(serverScriptResultTo);
            if (serverScriptResultTo != null && string.IsNullOrEmpty(serverScriptResultTo.Name))
                throw new InvalidOperationException("serverScriptResultTo必须是命名变量");
            //设置节点执行结果输出到变量
            if (serverResultTo != null)
                server.Result = new OutArgument<string>(serverResultTo);

            return server;
        }
        #endregion

        #region Parallel
        /// <summary>
        /// 创建并行节点
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="displayName"></param>
        /// <param name="completionScript"></param>
        /// <param name="next"></param>
        /// <param name="branchs"></param>
        /// <returns></returns>
        public static FlowStep CreateParallel(ActivitySetting setting
            , string displayName
            , string completionScript
            , FlowNode next
            , params Custom[] branchs)
        {
            return new FlowStep() { Action = CreateCustomParallel(setting, displayName, completionScript, branchs), Next = next };
        }
        /// <summary>
        /// 创建并行节点
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="displayName"></param>
        /// <param name="completionScript"></param>
        /// <param name="branchs"></param>
        /// <returns></returns> 
        public static CustomParallel CreateCustomParallel(ActivitySetting setting
            , string displayName
            , string completionScript
            , params Custom[] branchs)
        {
            var first = setting.FlowNodeIndex == Default_FlowNodeIndex;
            var p = first ? new CustomParallel() : new CustomParallel(setting.FlowNodeIndex);
            if (first) p.OnFlowNodeIndex = o => setting.SetFlowNodeIndex(o);

            p.DisplayName = displayName;
            if (branchs != null)
                branchs.ToList().ForEach(o => p.Branches.Add(o));
            //HACK:【重要】并行节点完成脚本执行设置CompletionCondition
            if (!string.IsNullOrEmpty(completionScript))
                p.CompletionCondition = new LambdaValue<bool>(o =>
                    o.Resolve<IScriptParser>().EvaluateRule(completionScript, o.GetExtension<DataFieldExtension>()));
            return p;
        }
        #endregion

        #region SubProcess
        /// <summary>
        /// 创建SubProcess子流程节点
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="displayName"></param>
        /// <param name="serverScript">节点执行内容脚本</param>
        /// <param name="finishRule">节点完成规则</param>
        /// <param name="serverScriptResultTo">执行内容的结果输出到指定变量</param>
        /// <param name="serverResultTo">节点执行结果输出到变量</param>
        /// <param name="nexts"></param>
        /// <param name="defaultFlowNode"></param>
        /// <returns></returns>
        public static FlowStep CreateSubProcess(ActivitySetting setting
            , string displayName
            , IDictionary<string, string> finishRule
            , Variable<string> resultTo
            , IDictionary<string, FlowNode> nexts
            , FlowNode defaultFlowNode)
        {
            var server = CreateSubProcess(setting, displayName, finishRule, resultTo);
            var step = new FlowStep();
            step.Action = server;

            if (nexts == null && defaultFlowNode == null) return step;

            //设置finish cases
            var flowSwitch = new FlowSwitch<string>(o => resultTo.Get(o));
            if (defaultFlowNode != null)
                flowSwitch.Default = defaultFlowNode;
            if (nexts != null)
                nexts.ToList().ForEach(o => flowSwitch.Cases.Add(o.Key, o.Value));
            step.Next = flowSwitch;
            return step;
        }
        /// <summary>
        /// 创建SubProcess子流程节点
        /// </summary>
        /// <param name="setting"></param>
        /// <param name="displayName"></param>
        /// <param name="finishRule">节点完成规则</param>
        /// <param name="resultTo">节点执行结果输出到变量</param>
        /// <returns></returns>
        public static SubProcess CreateSubProcess(ActivitySetting setting
            , string displayName
            , IDictionary<string, string> finishRule
            , Variable<string> resultTo)
        {
            var first = setting.FlowNodeIndex == Default_FlowNodeIndex;
            var sub = first ? new SubProcess() : new SubProcess(setting.FlowNodeIndex);
            if (first) sub.OnFlowNodeIndex = o => setting.SetFlowNodeIndex(o);

            sub.DisplayName = displayName;
            sub.FinishRule = finishRule;

            //设置节点执行结果输出到变量
            if (resultTo != null)
                sub.Result = new OutArgument<string>(resultTo);

            return sub;
        }
        #endregion

        //TODO:优化静态序列化方法设计
        public static string Serialize(object obj, Type target)
        {
            if (target == null || obj == null) return "null";
            using (var stream = new MemoryStream())
            using (var reader = new StreamReader(stream))
            {
                WorkflowBuilder.CreateSerializer(target).WriteObject(stream, obj);
                stream.Position = 0;
                return reader.ReadToEnd();
            }
        }
        public static object Deserialize(Type target, string input)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(input)))
                return WorkflowBuilder.CreateSerializer(target).ReadObject(stream);
        }
        private static DataContractSerializer CreateSerializer(Type target)
        {
            return new DataContractSerializer(target, WorkflowBuilder.GetKnownTypes(), 2147483646, false, true, null);
        }
        private static IEnumerable<Type> GetKnownTypes()
        {
            yield return typeof(object[]);
            yield return typeof(string[]);
        }
    }
}