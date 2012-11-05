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
using System.Linq;
using System.Text;
using Taobao.Workflow.Activities;
using System.Xml.Linq;
using System.Reflection;
using Taobao.Workflow.Activities.Statements;
using System.Text.RegularExpressions;
using Taobao.Activities;
using CodeSharp.Core.Services;

namespace Taobao.Workflow.Activities.Converters
{
    /// <summary>
    /// 默认转换器
    /// 【HACK】主要针对新设计器的流程描述文本的转换
    /// </summary>
    public class DefaultConverter : ConverterBase, IWorkflowConverter
    {
        //WF4的ID前缀格式
        private static readonly string _activityidPrefix = "__ReferenceID";

        string IWorkflowConverter.Parse(string workflowDefinition, string activitySettingsDefinition)
        {
            return null;
        }
        IList<ActivitySetting> IWorkflowConverter.ParseActivitySettings(string workflowDefinition, string activitySettingsDefinition)
        {
            var root = XElement.Parse(workflowDefinition);  
            //变量筛选到设计器变量
            var variableElements = root.Descendants("Variable")
                .Where(o => o.Attribute("Mode") != null && o.Attribute("Mode").Value.Trim() == "design")
                .ToList();
            //解析ActivitySettings
            var activitySettings = this.ParseActivitySettings(activitySettingsDefinition, variableElements);
            this.ParseWorkflowActivity(root, activitySettings, true);
            return activitySettings;
        }
        WorkflowActivity IWorkflowConverter.Parse(string workflowDefinition, IList<ActivitySetting> activitySettings)
        {
            var root = XElement.Parse(workflowDefinition);
            return this.ParseWorkflowActivity(root, activitySettings, false);
        }
        string IWorkflowConverter.Parse(WorkflowActivity activity, string originalWorkflowDefinition)
        {
            if (activity == null)
                throw new ArgumentException("activity is null.");

            #region 记录位置信息
            //判断原始格式是否是Xaml格式
            var isXaml = this.CheckIsXamlFormat(originalWorkflowDefinition);       
            var location_dict = new Dictionary<string, string>();
            IList<XElement> designVariableElements = new List<XElement>();
            var element = XElement.Parse(originalWorkflowDefinition);
            if (isXaml)
            {
                var elements = element.DescendantNodes()
                    .Select(o => o as XElement)
                    .Where(o => o != null);
                //解析活动节点
                var activityNode_elements = elements.Where(o => o.Name.LocalName == "FlowSwitch"
                    || o.Name.LocalName == "FlowStep").ToList();
                foreach (var activityNode_element in activityNode_elements)
                {
                    var viewState_element = activityNode_element.Nodes()
                    .Select(o => o as XElement)
                    .Where(o => o.Name.LocalName == "WorkflowViewStateService.ViewState")
                    .FirstOrDefault();
                    var point_element = viewState_element.DescendantNodes()
                        .Select(o => o as XElement)
                        .Where(o => o != null && o.Name.LocalName == "Point")
                        .FirstOrDefault();
                    string parallelName = "";
                    if (activityNode_element.Name.LocalName == "FlowStep")
                    {
                        var activityNode_element_nodes = activityNode_element.Nodes().Select(o => o as XElement).Where(o => o != null).ToList();
                        //获取并行容器节点
                        var parallel_element = activityNode_element_nodes.FirstOrDefault(o => o.Name.LocalName == "Parallel");
                        if (parallel_element != null)
                        {
                            parallelName = parallel_element.Attribute("DisplayName").Value;
                        }
                    }
                    //设置位置信息
                    location_dict[activityNode_element.Name.LocalName == "FlowSwitch"
                        ? ReplaceExpression(activityNode_element.Attribute("Expression").Value) 
                            : parallelName] = point_element.Value.Replace(",", " ");
                }

                //UNDONE:目前WF4格式以及originalWorkflowDefinition无法得到设计器变量
            }
            else
            {
                var nodeDataElements = element.Descendants("NodeData")
                    .Where(o => o.Attribute("Category").Value != "Comment")
                    .ToList();

                foreach (var nodeDataElement in nodeDataElements)
                {
                    var category = nodeDataElement.Attribute("Category").Value;
                    if (category == "HumanPar" || category == "ServerPar" || category == "ChildPar")
                        continue;
                    var activityName = nodeDataElement.Attribute("Text").Value;
                    var location = nodeDataElement.Attribute("Location") != null ? nodeDataElement.Attribute("Location").Value : "308 0";
                    //设置位置信息
                    location_dict[activityName] = location;
                }

                //获取设计器变量
                var variableElements = element.Descendants("Variable");
                variableElements.Where(o => o.Attribute("Mode").Value.ToLower() == "design").ToList().ForEach(o =>
                {
                    designVariableElements.Add(new XElement("Variable"
                        , new XAttribute("Name", o.Attribute("Name").Value)
                        , new XAttribute("Value", o.Attribute("Value").Value)
                        , new XAttribute("Mode", "design")));
                });
            }
            #endregion

            var root = new XElement("FlowChart");
            var variablesElement = new XElement("Variables");
            //添加变量节点
            foreach (var variable in activity.Variables)
                variablesElement.Add(new XElement("Variable"
                    , new XAttribute("Name", variable.Name)
                    , new XAttribute("Value", "")
                    , new XAttribute("Mode", "flow")));
            //遍历添加设计器变量
            foreach (var variable in designVariableElements)
                variablesElement.Add(variable);
            root.Add(variablesElement);

            //添加开始节点
            root.Add(new XElement("NodeData"
                , new XAttribute("Key", "Start")
                , new XAttribute("Category", "Start")
                , new XAttribute("Location", location_dict.ContainsKey("Start") ? location_dict["Start"] : "308 0")
                , new XAttribute("Text", "Start")));

            var dict = new Dictionary<Taobao.Activities.Statements.FlowStep, string>();
            int counter = 0;
            foreach (var node in activity.Body.Nodes)
            {
                if (node is Taobao.Activities.Statements.FlowStep)
                {
                    var flowStep = node as Taobao.Activities.Statements.FlowStep;
                    
                    //判断节点是否为NTFE-BPM平行节点
                    if(flowStep.Action is Taobao.Workflow.Activities.Statements.CustomParallel)
                    {
                        var parallel = flowStep.Action as Taobao.Workflow.Activities.Statements.CustomParallel;
                        int parallel_counter = counter;
                        var parallel_key = _activityidPrefix + (counter++);
                        //构造平行容器类型节点
                        root.Add(new XElement("NodeData"
                            , new XAttribute("Key", parallel_key)
                            , new XAttribute("Category", "ParallelContainer")
                            , new XAttribute("IsSubGraph", true)
                            , new XAttribute("Location", location_dict.ContainsKey(parallel.DisplayName) 
                                ? location_dict[parallel.DisplayName] : "600 0")
                            , new XAttribute("Text", parallel.DisplayName)));
                        int[] array = location_dict.ContainsKey(parallel.DisplayName)
                            ? location_dict[parallel.DisplayName].Split(' ').Select(o => Convert.ToInt32(o)).ToArray() : new int[] { 600, 0 };
                        dict.Add(flowStep, parallel_key);
                        int c = 0;
                        //遍历分支
                        foreach(var p_activity in parallel.Branches)
                        {
                            if (p_activity is Taobao.Workflow.Activities.Statements.Human)
                            {
                                var p_human = p_activity as Taobao.Workflow.Activities.Statements.Human;
                                //构造平行容器中的人工节点
                                root.Add(new XElement("NodeData"
                                    , new XAttribute("Key", _activityidPrefix + (counter++))
                                    , new XAttribute("Category", "HumanPar")
                                    , new XAttribute("IsSubGraph", true)
                                    , new XAttribute("SubGraphKey", _activityidPrefix + parallel_counter)
                                    , new XAttribute("Location", (array[0] + (c++) * 105) + " " + array[1])
                                    , new XAttribute("Text", p_human.DisplayName)));
                            }
                            else if (p_activity is Taobao.Workflow.Activities.Statements.Server)
                            {
                                var p_server = p_activity as Taobao.Workflow.Activities.Statements.Server;
                                //构造平行容器中的自动节点
                                root.Add(new XElement("NodeData"
                                    , new XAttribute("Key", _activityidPrefix + (counter++))
                                    , new XAttribute("Category", "ServerPar")
                                    , new XAttribute("IsSubGraph", true)
                                    , new XAttribute("SubGraphKey", _activityidPrefix + parallel_counter)
                                    , new XAttribute("Location", (array[0] + (c++) * 105) + " " + array[1])
                                    , new XAttribute("Text", p_server.DisplayName)));
                            }
                            else if (p_activity is Taobao.Workflow.Activities.Statements.SubProcess)
                            {
                                var p_subProcess = p_activity as Taobao.Workflow.Activities.Statements.SubProcess;
                                //构造平行容器中的子流程节点
                                root.Add(new XElement("NodeData"
                                    , new XAttribute("Key", _activityidPrefix + (counter++))
                                    , new XAttribute("Category", "ChildPar")
                                    , new XAttribute("IsSubGraph", true)
                                    , new XAttribute("SubGraphKey", _activityidPrefix + parallel_counter)
                                    , new XAttribute("Location", (array[0] + (c++) * 105) + " " + array[1])
                                    , new XAttribute("Text", p_subProcess.DisplayName)));
                            }
                        }
                    }
                    else if (flowStep.Action is Taobao.Workflow.Activities.Statements.Human)
                    {
                        var human = flowStep.Action as Taobao.Workflow.Activities.Statements.Human;
                        var key = _activityidPrefix + (counter++);
                        //构造人工节点
                        root.Add(new XElement("NodeData"
                                    , new XAttribute("Key", key)
                                    , new XAttribute("Category", "Human")
                                    , new XAttribute("IsSubGraph", true)
                                    , new XAttribute("Location", location_dict.ContainsKey(human.DisplayName) 
                                        ? location_dict[human.DisplayName] : "600 0")
                                    , new XAttribute("Text", human.DisplayName)));
                        dict.Add(flowStep, key);
                    }
                    else if (flowStep.Action is Taobao.Workflow.Activities.Statements.Server)
                    {
                        var server = flowStep.Action as Taobao.Workflow.Activities.Statements.Server;
                        var key = _activityidPrefix + (counter++);
                        //构造自动节点
                        root.Add(new XElement("NodeData"
                                    , new XAttribute("Key", key)
                                    , new XAttribute("Category", "Server")
                                    , new XAttribute("IsSubGraph", true)
                                    , new XAttribute("Location", location_dict.ContainsKey(server.DisplayName) 
                                        ? location_dict[server.DisplayName] : "600 0")
                                    , new XAttribute("Text", server.DisplayName)));
                        dict.Add(flowStep, key);
                    }
                    else if (flowStep.Action is Taobao.Workflow.Activities.Statements.SubProcess)
                    {
                        var subProcess = flowStep.Action as Taobao.Workflow.Activities.Statements.SubProcess;
                        var key = _activityidPrefix + (counter++);
                        //构造子流程节点
                        root.Add(new XElement("NodeData"
                                    , new XAttribute("Key", key)
                                    , new XAttribute("Category", "Child")
                                    , new XAttribute("IsSubGraph", true)
                                    , new XAttribute("Location", location_dict.ContainsKey(subProcess.DisplayName) 
                                        ? location_dict[subProcess.DisplayName] : "600 0")
                                    , new XAttribute("Text", subProcess.DisplayName)));
                        dict.Add(flowStep, key);
                    }
                }
            }

            if (activity.Body.StartNode != null
                && activity.Body.StartNode is Taobao.Activities.Statements.FlowStep)
            {
                var startFlowStep = activity.Body.StartNode as Taobao.Activities.Statements.FlowStep;
                var toKey = dict[startFlowStep];
                //构造开始连线
                root.Add(new XElement("LinkData"
                    , new XAttribute("From", "Start")
                    , new XAttribute("To", toKey)
                    , new XAttribute("IsDefault", true)));
            }

            foreach (var node in activity.Body.Nodes)
            {
                if (node is Taobao.Activities.Statements.FlowStep)
                {
                    var flowStep = node as Taobao.Activities.Statements.FlowStep;
                    var fromKey = dict[flowStep];
                    if (flowStep.Next != null)
                    {
                        var flowSwitch = flowStep.Next as Taobao.Activities.Statements.FlowSwitch<string>;
                        foreach (var caseNode in flowSwitch.Cases)
                        {
                            var linkText = caseNode.Key;
                            if (caseNode.Value is Taobao.Activities.Statements.FlowStep)
                            {
                                var caseFlowStep = caseNode.Value as Taobao.Activities.Statements.FlowStep;
                                var toKey = dict[caseFlowStep];
                                //构造case连线
                                root.Add(new XElement("LinkData"
                                    , new XAttribute("From", fromKey)
                                    , new XAttribute("To", toKey)
                                    , new XAttribute("Text", linkText)
                                    , new XAttribute("IsDefault", false)));
                            }
                        }
                        if (flowSwitch.Default != null 
                            && flowSwitch.Default is Taobao.Activities.Statements.FlowStep)
                        {
                            var defaultFlowStep = flowSwitch.Default as Taobao.Activities.Statements.FlowStep;
                            var toKey = dict[defaultFlowStep];
                            //构造默认连线
                            root.Add(new XElement("LinkData"
                                    , new XAttribute("From", fromKey)
                                    , new XAttribute("To", toKey)
                                    , new XAttribute("Text", "Default")
                                    , new XAttribute("IsDefault", true)));
                        }
                    }
                }
            }
            return root.ToString();
        }

        //通过自定义活动文本描述解析自定义活动列表 用于首次发布时解析
        private IList<ActivitySetting> ParseActivitySettings(string activitySettingsDefinition
            , List<XElement> variableElements)
        {
            var timeZoneService = DependencyResolver.Resolve<ITimeZoneService>();

            var activitySettings = new List<ActivitySetting>();
            var rootElement = XElement.Parse(activitySettingsDefinition);
            rootElement.Descendants("human").ToList().ForEach(element =>
            {
                #region human
                var activityName = element.Attribute("name").Value;
                var actions = element.Descendants("actions")
                    .Descendants("action")
                    .Select(o => o.Value)
                    .ToList();
                var actioners = element.Descendants("actioners")
                    .Descendants("actioner")
                    .Select(o => o.Value)
                    .ToArray();
                var actionerRules = actioners;
                var slotCount = element.Attribute("slot") != null ? element.Attribute("slot").Value : "-1";
                var slotMode = element.Attribute("slotMode") != null ? element.Attribute("slotMode").Value : "allatonce";

                var url = element.Attribute("url") != null ? element.Attribute("url").Value : string.Empty;
                //HACK:设计器变量替换url占位符
                variableElements.ForEach(variableElement => 
                {
                    url = url.Replace("{" + variableElement.Attribute("Name").Value + "}", variableElement.Attribute("Value").Value);
                });

                #region 开始规则解析
                string startRule_At = null, startRule_afterMinutes = null, startRule_timeZoneName = null, startRule_unlimited = null;
                Nullable<DateTime> nullTime = null;
                Nullable<Int32> nullInt = null;
                Nullable<Double> nullDouble = null;
                StartRule startRule = null;
                if (element.Element("start") != null)
                {
                    var startRuleElement = element.Element("start");
                    startRule_At = startRuleElement.Attribute("at") != null ? startRuleElement.Attribute("at").Value : null;
                    startRule_afterMinutes = startRuleElement.Attribute("after") != null ? startRuleElement.Attribute("after").Value : null;
                    startRule_timeZoneName = startRuleElement.Attribute("zone") != null ? startRuleElement.Attribute("zone").Value : null;
                    //无限期延迟标记
                    startRule_unlimited = startRuleElement.Attribute("unlimited") != null ? startRuleElement.Attribute("unlimited").Value : null;
                    bool unlimited;
                    if (bool.TryParse(startRule_unlimited, out unlimited) && unlimited)
                        startRule = StartRule.UnlimitedDelay();
                    else if (startRule_At != null || startRule_afterMinutes != null || startRule_timeZoneName != null)
                        startRule = new StartRule(!string.IsNullOrEmpty(startRule_At) ? Convert.ToDateTime(startRule_At) : nullTime
                            , !string.IsNullOrEmpty(startRule_afterMinutes) ? Convert.ToInt32(startRule_afterMinutes) : nullInt
                            , string.IsNullOrEmpty(startRule_timeZoneName) ? null : timeZoneService.GetTimeZone(startRule_timeZoneName));
                }
                #endregion

                #region 完成规则解析
                var scripts = element.Descendants("finish")
                    .Descendants("line")
                    .ToDictionary(o => o.Attribute("name").Value
                        , o => o.Value);
                #endregion

                bool isChildOfActivity;
                bool.TryParse(element.Attribute("isChildOfActivity") == null
                    ? "False" : element.Attribute("isChildOfActivity").Value, out isChildOfActivity);

                #region 超时升级规则解析
                HumanEscalationRule escalationRule = null;
                if (element.Element("escalation") != null)
                {
                    var escalationRuleElement = element.Element("escalation");
                    string escalationRule_timeZoneName = escalationRuleElement.Attribute("zone") != null ? escalationRuleElement.Attribute("zone").Value : null;
                    TimeZone escalationRuleTimeZone = null;
                    if (!string.IsNullOrEmpty(escalationRule_timeZoneName))
                        escalationRuleTimeZone = timeZoneService.GetTimeZone(escalationRule_timeZoneName);
                    var escalationRule_ExpirationMinute = escalationRuleElement.Attribute("expiration") != null ? escalationRuleElement.Attribute("expiration").Value : null;
                    double? expirationMinute = !string.IsNullOrEmpty(escalationRule_ExpirationMinute) ? Convert.ToDouble(escalationRule_ExpirationMinute) : nullDouble;
                    var escalationRule_NotifyRepeatMinutes = escalationRuleElement.Attribute("repeat") != null ? escalationRuleElement.Attribute("repeat").Value : null;
                    double? repeatMinutes = !string.IsNullOrEmpty(escalationRule_NotifyRepeatMinutes) ? Convert.ToDouble(escalationRule_NotifyRepeatMinutes) : nullDouble;
                    var escalationRule_NotifyEmailTemplateName = escalationRuleElement.Attribute("notifyTemplateName") != null && !string.IsNullOrEmpty(escalationRuleElement.Attribute("notifyTemplateName").Value) ? escalationRuleElement.Attribute("notifyTemplateName").Value : null;
                    var escalationRule_GotoActivityName = escalationRuleElement.Attribute("gotoActivityName") != null && !string.IsNullOrEmpty(escalationRuleElement.Attribute("gotoActivityName").Value) ? escalationRuleElement.Attribute("gotoActivityName").Value : null;
                    var escalationRule_RedirectTo = escalationRuleElement.Attribute("redirectTo") != null && !string.IsNullOrEmpty(escalationRuleElement.Attribute("redirectTo").Value) ? escalationRuleElement.Attribute("redirectTo").Value : null;
                    //构造超时升级规则
                    escalationRule = new HumanEscalationRule(expirationMinute
                        , escalationRuleTimeZone
                        , repeatMinutes
                        , escalationRule_NotifyEmailTemplateName
                        , escalationRule_GotoActivityName
                        , escalationRule_RedirectTo);
                }
                #endregion

                activitySettings.Add(new HumanSetting(WorkflowBuilder.Default_FlowNodeIndex//HACK:设置默认索引，用于临时标记
                    , activityName
                    , actions.ToArray()
                    , Convert.ToInt32(slotCount)
                    , slotMode.ToLower() == "oneatonce" ? HumanSetting.SlotDistributionMode.OneAtOnce : HumanSetting.SlotDistributionMode.AllAtOnce
                    , url
                    , startRule
                    , new HumanActionerRule(actionerRules)
                    , new FinishRule(scripts)
                    , escalationRule
                    , isChildOfActivity));
                #endregion
            });
            rootElement.Descendants("server").ToList().ForEach(element =>
            {
                #region server
                var activityName = element.Attribute("name").Value;
                string startRule_At = null, startRule_afterMinutes = null, startRule_timeZoneName = null, startRule_unlimited = null;
                Nullable<DateTime> nullTime = null;
                Nullable<Int32> nullInt = null;
                StartRule startRule = null;
                if (element.Element("start") != null)
                {
                    var startRuleElement = element.Element("start");
                    startRule_At = startRuleElement.Attribute("at") != null ? startRuleElement.Attribute("at").Value : null;
                    startRule_afterMinutes = startRuleElement.Attribute("after") != null ? startRuleElement.Attribute("after").Value : null;
                    startRule_timeZoneName = startRuleElement.Attribute("zone") != null ? startRuleElement.Attribute("zone").Value : null;
                    //无限期延迟标记
                    startRule_unlimited = startRuleElement.Attribute("unlimited") != null ? startRuleElement.Attribute("unlimited").Value : null;
                    bool unlimited;
                    if (bool.TryParse(startRule_unlimited, out unlimited) && unlimited)
                        startRule = StartRule.UnlimitedDelay();
                    else if (startRule_At != null || startRule_afterMinutes != null || startRule_timeZoneName != null)
                        startRule = new StartRule(!string.IsNullOrEmpty(startRule_At) ? Convert.ToDateTime(startRule_At) : nullTime
                            , !string.IsNullOrEmpty(startRule_afterMinutes) ? Convert.ToInt32(startRule_afterMinutes) : nullInt
                            , string.IsNullOrEmpty(startRule_timeZoneName) ? null : timeZoneService.GetTimeZone(startRule_timeZoneName));
                }

                var scripts = element.Descendants("finish")
                    .Descendants("line")
                    .ToDictionary(o => o.Attribute("name").Value
                        , o => o.Value);
                var serverScript = element.Element("script") != null ? element.Element("script").Value : string.Empty;
                var resultTo = element.Attribute("resultTo") != null ? element.Attribute("resultTo").Value : string.Empty;

                bool isChildOfActivity;
                bool.TryParse(element.Attribute("isChildOfActivity") == null
                    ? "False" : element.Attribute("isChildOfActivity").Value, out isChildOfActivity);

                activitySettings.Add(new ServerSetting(WorkflowBuilder.Default_FlowNodeIndex
                    , activityName
                    , serverScript
                    , resultTo
                    , startRule
                    , new FinishRule(scripts)
                    , isChildOfActivity));
                #endregion
            });
            rootElement.Descendants("subprocess").ToList().ForEach(element =>
            {
                #region subprocess
                var activityName = element.Attribute("name").Value;
                var subProcessTypeName = element.Attribute("processTypeName").Value;

                string startRule_At = null, startRule_afterMinutes = null, startRule_timeZoneName = null, startRule_unlimited = null;
                Nullable<DateTime> nullTime = null;
                Nullable<Int32> nullInt = null;
                StartRule startRule = null;
                if (element.Element("start") != null)
                {
                    var startRuleElement = element.Element("start");
                    startRule_At = startRuleElement.Attribute("at") != null ? startRuleElement.Attribute("at").Value : null;
                    startRule_afterMinutes = startRuleElement.Attribute("after") != null ? startRuleElement.Attribute("after").Value : null;
                    startRule_timeZoneName = startRuleElement.Attribute("zone") != null ? startRuleElement.Attribute("zone").Value : null;
                    //无限期延迟标记
                    startRule_unlimited = startRuleElement.Attribute("unlimited") != null ? startRuleElement.Attribute("unlimited").Value : null;
                    bool unlimited;
                    if (bool.TryParse(startRule_unlimited, out unlimited) && unlimited)
                        startRule = StartRule.UnlimitedDelay();
                    else if (startRule_At != null || startRule_afterMinutes != null || startRule_timeZoneName != null)
                        startRule = new StartRule(!string.IsNullOrEmpty(startRule_At) ? Convert.ToDateTime(startRule_At) : nullTime
                            , !string.IsNullOrEmpty(startRule_afterMinutes) ? Convert.ToInt32(startRule_afterMinutes) : nullInt
                            , string.IsNullOrEmpty(startRule_timeZoneName) ? null : new Taobao.Workflow.Activities.TimeZone(startRule_timeZoneName));
                }

                var scripts = element.Descendants("finish")
                    .Descendants("line")
                    .ToDictionary(o => o.Attribute("name").Value
                        , o => o.Value);

                bool isChildOfActivity;
                bool.TryParse(element.Attribute("isChildOfActivity") == null
                    ? "False" : element.Attribute("isChildOfActivity").Value, out isChildOfActivity);

                activitySettings.Add(new SubProcessSetting(WorkflowBuilder.Default_FlowNodeIndex//HACK:设置默认索引，用于临时标记
                    , subProcessTypeName
                    , activityName
                    , startRule
                    , new FinishRule(scripts)
                    , isChildOfActivity));
                #endregion
            });
            rootElement.Descendants("parallel").ToList().ForEach(element =>
            {
                #region parallel
                var activityName = element.Attribute("name").Value;
                var completionCondition = element.Element("condition") != null ? element.Element("condition").Value : string.Empty;
                bool isChildOfActivity;
                bool.TryParse(element.Attribute("isChildOfActivity") == null
                    ? "False" : element.Attribute("isChildOfActivity").Value, out isChildOfActivity);
                activitySettings.Add(new ParallelSetting(WorkflowBuilder.Default_FlowNodeIndex
                    , activityName
                    , completionCondition
                    , isChildOfActivity));
                #endregion
            });
            return activitySettings;
        }
        //通过Flowchart节点和自定义活动列表解析NTFE-BPM活动树
        private Taobao.Workflow.Activities.Statements.WorkflowActivity ParseWorkflowActivity(XElement root
            , IList<ActivitySetting> activitySettings
            , bool isCacheMetaData)
        {
            var activity = new WorkflowActivity();
            
            var variableElements = root.Descendants("Variable").ToList();
            //设置变量集合
            this.SetVariablesForActivity(variableElements, activity);

            var nodeDataElements = root.Descendants("NodeData")
                .Where(o => o.Attribute("Category").Value != "Comment")
                .ToList();
            var linkDataElements = root.Descendants("LinkData").ToList();

            var flowNodes = new Dictionary<string, FlowNodeInfo>(); //定义点集合

            var temp = new List<string>();
            foreach (var nodeDataElement in nodeDataElements)
            {
                var category = nodeDataElement.Attribute("Category").Value;
                if (category == "HumanPar" || category == "ServerPar" || category == "ChildPar" || category == "Start")
                    continue;
                var activityName = nodeDataElement.Attribute("Text").Value;
                var key = nodeDataElement.Attribute("Key").Value;
                if (temp.Contains(activityName))
                    throw new InvalidOperationException("流程中包括重复名称的节点，请检查！");
                temp.Add(activityName);
                var activitySetting = activitySettings.FirstOrDefault(o => o.ActivityName == activityName);
                if (activitySetting == null)
                    throw new Exception(string.Format(@"解析出错，找不到名称为“{0}”的ActivitySetting", activityName));
                Taobao.Activities.Statements.FlowStep flowStep = null;
                if (activitySetting is HumanSetting)
                {
                    var humanSetting = activitySetting as HumanSetting;
                    //创建人工节点
                    flowStep = WorkflowBuilder.CreateHuman(activitySetting
                            , activitySetting.ActivityName
                            , new GetUsers(humanSetting.ActionerRule.Scripts)
                            , activity.CustomActivityResult
                            , new Dictionary<string, Taobao.Activities.Statements.FlowNode>()
                            , null);
                }
                else if (activitySetting is ServerSetting)
                {
                    var customSetting = activitySetting as CustomSetting;
                    var serverSetting = activitySetting as ServerSetting;
                    var resultTo = activity.Variables.ToList().FirstOrDefault(o => o.Name == serverSetting.ResultTo);
                    //创建人工节点
                    flowStep = WorkflowBuilder.CreateServer(activitySetting
                           , activitySetting.ActivityName
                           , serverSetting.Script
                           , customSetting.FinishRule.Scripts
                           , resultTo != null ? (Variable<string>)resultTo : null
                           , activity.CustomActivityResult
                           , new Dictionary<string, Taobao.Activities.Statements.FlowNode>()
                           , null);
                }
                else if (activitySetting is SubProcessSetting)
                {
                    var customSetting = activitySetting as CustomSetting;
                    var subProcessSetting = activitySetting as SubProcessSetting;
                    //创建子流程节点
                    flowStep = WorkflowBuilder.CreateSubProcess(activitySetting
                            , activitySetting.ActivityName
                            , customSetting.FinishRule.Scripts
                            , activity.CustomActivityResult
                            , new Dictionary<string, Taobao.Activities.Statements.FlowNode>()
                            , null);
                }
                else if (activitySetting is ParallelSetting)
                {
                    IList<Custom> customs = new List<Custom>();
                    //遍历customs
                    var customParElements = nodeDataElements.Where(o => o.Attribute("SubGraphKey") != null && o.Attribute("SubGraphKey").Value == key).ToList();
                    foreach (var customParElement in customParElements)
                    {
                        var customeCategory = customParElement.Attribute("Category").Value;
                        var customeActivityName = customParElement.Attribute("Text").Value;
                        if (temp.Contains(customeActivityName))
                            throw new InvalidOperationException("流程中包括重复名称的节点，请检查！");
                        temp.Add(customeActivityName);
                        var customActivitySetting = activitySettings.FirstOrDefault(o => o.ActivityName == customeActivityName);
                        if (customActivitySetting == null)
                            throw new Exception(string.Format(@"解析出错，找不到名称为“{0}”的ActivitySetting", customeActivityName));
                        if (customActivitySetting is HumanSetting)
                        {
                            var humanSetting = customActivitySetting as HumanSetting;
                            //创建并行容器中人工节点
                            var human = WorkflowBuilder.CreateHuman(customActivitySetting
                                , customActivitySetting.ActivityName
                                , new GetUsers(humanSetting.ActionerRule.Scripts)
                                , activity.CustomActivityResult);
                            customs.Add(human);
                        }
                        else if (customActivitySetting is ServerSetting)
                        {
                            var customSetting = customActivitySetting as CustomSetting;
                            var serverSetting = customActivitySetting as ServerSetting;
                            var resultTo = activity.Variables.ToList().FirstOrDefault(o => o.Name == serverSetting.ResultTo);
                            //创建并行容器中自动节点
                            var server = WorkflowBuilder.CreateServer(customActivitySetting
                                , customActivitySetting.ActivityName
                                , serverSetting.Script
                                , customSetting.FinishRule.Scripts
                                , resultTo != null ? (Variable<string>)resultTo : null
                                , activity.CustomActivityResult);
                            customs.Add(server);
                        }
                        else if (customActivitySetting is SubProcessSetting)
                        {
                            var customSetting = customActivitySetting as CustomSetting;
                            var subProcessSetting = customActivitySetting as SubProcessSetting;
                            //创建并行容器中子流程节点
                            var subprocess = WorkflowBuilder.CreateSubProcess(customActivitySetting
                            , customActivitySetting.ActivityName
                            , customSetting.FinishRule.Scripts
                            , activity.CustomActivityResult);
                            customs.Add(subprocess);
                        }
                    }
                    //创建并行容器节点
                    flowStep = WorkflowBuilder.CreateParallel(activitySetting
                            , activitySetting.ActivityName
                            , (activitySetting as ParallelSetting).CompletionCondition
                            , null
                            , customs.ToArray());
                }
                flowNodes.Add(key, new FlowNodeInfo() { FlowStep = flowStep });
            }

            if(nodeDataElements.Count(o => o.Attribute("Category").Value == "Start") != 1)
                throw new InvalidOperationException("不包括Start开始节点");

            //创建流程节点
            this.BuildFlowNode(flowNodes, linkDataElements);

            var startLinkElement = linkDataElements.FirstOrDefault(o => o.Attribute("From").Value == "Start");
            if (startLinkElement == null)
                throw new InvalidOperationException("不包含Start到初始点连线");
            activity.Body.StartNode = flowNodes[startLinkElement.Attribute("To").Value].FlowStep;

            //初始化Flowchart节点的元素
            if (isCacheMetaData)
                CacheMetadata(activity.Body);

            return activity;
        }
        //构建流程节点
        private void BuildFlowNode(Dictionary<string, FlowNodeInfo> flowNodes, List<XElement> linkDataElements)
        {
            foreach (var linkDataElement in linkDataElements)
            {
                var fromKey = linkDataElement.Attribute("From").Value;
                if (fromKey == "Start")
                    continue;
                var toKey = linkDataElement.Attribute("To").Value;
                var linkText = linkDataElement.Attribute("Text") != null ? linkDataElement.Attribute("Text").Value : "";
                var flowSwitch = flowNodes[fromKey].FlowStep.Next as Taobao.Activities.Statements.FlowSwitch<string>;
                var isDefault = linkDataElement.Attribute("IsDefault") != null 
                    ? Convert.ToBoolean(linkDataElement.Attribute("IsDefault").Value) : true;
                if (isDefault)
                {
                    //HACK:为空时，处理并行容器节点
                    if(flowSwitch == null)
                        flowNodes[fromKey].FlowStep.Next = flowNodes[toKey].FlowStep;
                    else
                        flowSwitch.Default = flowNodes[toKey].FlowStep;
                }
                else
                {
                    if (flowSwitch.Cases.ContainsKey(linkText))
                        throw new InvalidOperationException(string.Format("“{0}”和“{1}”之间存在重复连线的名称“{2}”", fromKey, toKey, linkText));
                    flowSwitch.Cases.Add(linkText, flowNodes[toKey].FlowStep);
                }
            }
        }
        //设置变量集合
        private void SetVariablesForActivity(List<XElement> variableElments
            , WorkflowActivity activity)
        {
            foreach (var variableElment in variableElments)
            {
                //当变量为设计器变量时跳过
                if (variableElment.Attribute("Mode") != null && variableElment.Attribute("Mode").Value.Trim() == "design")
                    continue;
                var variableName = variableElment.Attribute("Name").Value;
                //判断变量名称是否合法
                if (Regex.IsMatch(variableName, @"[a-zA-Z][a-zA-Z_0-9]*"))
                    activity.Variables.Add(new Taobao.Activities.Variable<String>(variableName));
            }
        }
        //初始化元数据
        private static void CacheMetadata(Taobao.Activities.Statements.Flowchart flowchart)
        {
            var methodInfo = flowchart.GetType().GetMethod("CacheMetadata", BindingFlags.Instance
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly);
            methodInfo.Invoke(flowchart, null);
        }
        //过滤表达式
        private string ReplaceExpression(string input)
        {
            var match = Regex.Match(input, @"(.+?)->.*");
            if (match.Success)
                input = match.Groups[1].Value;
            return input;
        }
        //验证原始流程描述文本是否为Xaml格式
        private bool CheckIsXamlFormat(string workflowDefinition)
        {
            var root = XElement.Parse(workflowDefinition);
            return root.Name.LocalName != "FlowChart";
        }

        #region 相关子类
        /// <summary>
        /// 活动节点信息
        /// </summary>
        class FlowNodeInfo
        {
            /// <summary>
            /// 对应的NTFE的活动节点
            /// </summary>
            public Taobao.Activities.Statements.FlowStep FlowStep { get; set; }
        }
        #endregion
    }
}
