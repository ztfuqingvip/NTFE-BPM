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
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Taobao.Workflow.Activities;
using Taobao.Workflow.Activities.Statements;
using System.Reflection;
using Taobao.Activities;
using System.Activities.XamlIntegration;
using Taobao.Workflow.Activities.Designer.Statements;
using CodeSharp.Core.Services;

namespace Taobao.Workflow.Activities.Converters
{
    /// <summary>
    /// WF4文本转换器
    /// HACK:
    /// </summary>
    public class WF4Converter : ConverterBase, IWorkflowConverter
    {
        /// <summary>
        /// 最大深度
        /// </summary>
        private static readonly int MAX_DEPTN = 1000;

        //WF4相应命名空间名称
        private static readonly string _namespaceX = "http://schemas.microsoft.com/winfx/2006/xaml";

        /// <summary>
        /// HACK:【重要】Xaml+RuleXml -> New Xml 转换
        /// </summary>
        /// <param name="workflowDefinition"></param>
        /// <param name="activitySettingsDefinition"></param>
        /// <returns></returns>
        string IWorkflowConverter.Parse(string workflowDefinition, string activitySettingsDefinition)
        {
            var activitySettingElement = XElement.Parse(activitySettingsDefinition);
            IDictionary<string, string> dict = new Dictionary<string, string>();
            activitySettingElement.Descendants("human").ToList().ForEach(e =>
            {
                var activityName = e.Attribute("name").Value;
                dict[e.Attribute("name").Value] = "Human";
            });
            activitySettingElement.Descendants("server").ToList().ForEach(e =>
            {
                var activityName = e.Attribute("name").Value;
                dict[e.Attribute("name").Value] = "Server";
            });

            var element = XElement.Parse(workflowDefinition);

            var root = new XElement("FlowChart");

            var elements = element.DescendantNodes()
                .Select(o => o as XElement)
                .Where(o => o != null);

            //解析变量
            var variable_elements = elements.Where(o => o.Name.LocalName == "Variable").ToList();
            var variable_outputElements = new XElement("Variables"
                , variable_elements
                    .Select(o => new XElement("Variable"
                        , new XAttribute("Name", o.Attribute("Name").Value)
                        , new XAttribute("Value", ""))));
            //添加变量集合
            root.Add(variable_outputElements);
            //添加开始节点
            root.Add(new XElement("NodeData"
                , new XAttribute("Key", "Start")
                , new XAttribute("Category", "Start")
                , new XAttribute("Location", "308 0")
                , new XAttribute("Text", "Start")));
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
                    var parallel_element = activityNode_element_nodes.FirstOrDefault(o => o.Name.LocalName == "Parallel");
                    if (parallel_element != null)
                    {
                        parallelName = parallel_element.Attribute("DisplayName").Value;
                        var parallel_viewState_element = activityNode_element_nodes.FirstOrDefault(o => o.Name.LocalName == "WorkflowViewStateService.ViewState");
                        var parallel_point_element = parallel_viewState_element.DescendantNodes().Select(o => o as XElement).Where(o => o != null && o.Name.LocalName == "Point").FirstOrDefault();
                        var parallel_element_nodes = parallel_element.Nodes().Select(o => o as XElement).Where(o => o != null).ToList();
                        var activityPar_elements = parallel_element_nodes.Where(o => o.Name.LocalName == "Human" || o.Name.LocalName == "Server").ToList();

                        int[] array = parallel_point_element.Value.Split(',').Select(o => Convert.ToInt32(o)).ToArray();
                        int counter = 0;
                        foreach (var activityPar_element in activityPar_elements)
                        {
                            //添加并行节点中的活动
                            root.Add(new XElement("NodeData"
                                , new XAttribute("Key", activityNode_element.Attribute(XName.Get("Name", _namespaceX)).Value + "_" + activityPar_element.Attribute("DisplayName").Value)
                                , new XAttribute("Category", dict[activityPar_element.Attribute("DisplayName").Value] + "Par") 
                                , new XAttribute("IsSubGraph", true)
                                , new XAttribute("Location", (array[0] + (counter++) * 105) + " " + array[1])
                                , new XAttribute("SubGraphKey", activityNode_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                                , new XAttribute("Text", activityPar_element.Attribute("DisplayName").Value)));
                        }
                    }

                    var activityNode_nextlink_element = activityNode_element.Nodes().Select(o => o as XElement).FirstOrDefault(o => o.Name.LocalName == "FlowStep.Next");
                    if (activityNode_nextlink_element != null)
                    {
                        var to_element = activityNode_nextlink_element.FirstNode;
                        if (to_element != null)
                        {
                            //添加默认连线
                            root.Add(new XElement("LinkData"
                                , new XAttribute("From", activityNode_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                                , new XAttribute("To", (to_element as XElement).Attribute(XName.Get("Name", _namespaceX)).Value)
                                , new XAttribute("IsDefault", true)));
                        }
                    }
                }

                //添加节点
                var activityNode_outputElement = new XElement("NodeData"
                    , new XAttribute("Key", activityNode_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                    , new XAttribute("Category", activityNode_element.Name.LocalName == "FlowSwitch" ? dict[ReplaceExpression(activityNode_element.Attribute("Expression").Value)] : "ParallelContainer")
                    , new XAttribute("IsSubGraph", true)
                    , new XAttribute("Location", point_element.Value.Replace(",", " "))
                    , new XAttribute("Text", activityNode_element.Name.LocalName == "FlowSwitch"
                        ? ReplaceExpression(activityNode_element.Attribute("Expression").Value) : parallelName));
                root.Add(activityNode_outputElement);

                var activityNode_defaultlink_element = activityNode_element.Nodes()
                    .Select(o => o as XElement)
                    .FirstOrDefault(o => o.Name.LocalName == "FlowSwitch.Default");
                if (activityNode_defaultlink_element != null)
                {
                    var to_element = activityNode_defaultlink_element.FirstNode;
                    if (to_element != null)
                    {
                        //添加默认连线
                        root.Add(new XElement("LinkData"
                            , new XAttribute("From", activityNode_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                            , new XAttribute("To", (to_element as XElement).Attribute(XName.Get("Name", _namespaceX)).Value)
                            , new XAttribute("Text", "Default")
                            , new XAttribute("IsDefault", true)));
                    }
                }

                //WF4引用语法模式
                var activityNode_reflink_elements = activityNode_element.Nodes()
                    .Select(o => o as XElement)
                    .Where(o => o.Name.LocalName == "Reference");
                foreach (var activityNode_reflink_element in activityNode_reflink_elements)
                {
                    var nodes = activityNode_reflink_element.Nodes().ToList();
                    string to = "", text = "";
                    if (nodes.Count >= 1)
                    {
                        //引用模式中去除换行回车空格等字符
                        to = (nodes[0] as XText).Value.Replace("\r", "").Replace("\n", "").Replace(" ", "");
                    }
                    if (nodes.Count == 2)
                    {
                        text = (nodes[1] as XElement).Value;
                    }
                    //添加case连线
                    root.Add(new XElement("LinkData"
                            , new XAttribute("From", activityNode_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                            , new XAttribute("To", to)
                            , new XAttribute("Text", text)
                            , new XAttribute("IsDefault", false)));
                }

                var activityNode_link_elements = activityNode_element.Nodes()
                    .Select(o => o as XElement)
                    .Where(o => o.Name.LocalName == "FlowSwitch");
                foreach (var activityNode_link_element in activityNode_link_elements)
                {
                    //添加case连线
                    root.Add(new XElement("LinkData"
                            , new XAttribute("From", activityNode_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                            , new XAttribute("To", activityNode_link_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                            , new XAttribute("Text", activityNode_link_element.Attribute(XName.Get("Key", _namespaceX)).Value)
                            , new XAttribute("IsDefault", false)));
                }

                var default_attribute = activityNode_element.Attribute("Default");
                if (default_attribute != null)
                {
                    //添加默认连线
                    root.Add(new XElement("LinkData"
                            , new XAttribute("From", activityNode_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                            , new XAttribute("To", Regex.Replace(default_attribute.Value, @"^\{x:Reference (.+?)\}$", "$1", RegexOptions.IgnoreCase))
                            , new XAttribute("Text", "Default")
                            , new XAttribute("IsDefault", true)));
                }

                var activityNode_linkstep_element = activityNode_element.Nodes()
                    .Select(o => o as XElement)
                    .FirstOrDefault(o => o.Name.LocalName == "FlowStep");
                if (activityNode_linkstep_element != null)
                {
                    //添加case连线
                    root.Add(new XElement("LinkData"
                            , new XAttribute("From", activityNode_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                            , new XAttribute("To", activityNode_linkstep_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                            , new XAttribute("Text", activityNode_linkstep_element.Attribute(XName.Get("Key", _namespaceX)).Value)
                            , new XAttribute("IsDefault", false)));
                }
            }

            var startNode_element = elements.FirstOrDefault(o => o.Name.LocalName == "Flowchart.StartNode");
            if (startNode_element != null)
            {
                var startNode_activityNode_element = startNode_element.Nodes().Select(o => o as XElement)
                    .FirstOrDefault(o => o.Name.LocalName == "FlowSwitch" || o.Name.LocalName == "FlowStep");
                if (startNode_activityNode_element != null)
                {
                    //添加默认连线
                    root.Add(new XElement("LinkData"
                        , new XAttribute("From", "Start")
                        , new XAttribute("To", startNode_activityNode_element.Attribute(XName.Get("Name", _namespaceX)).Value)
                        , new XAttribute("IsDefault", true)));
                }
            }
            return root.ToString();
        }
        IList<ActivitySetting> IWorkflowConverter.ParseActivitySettings(string workflowDefinition, string activitySettingsDefinition)
        {
            //解析WF4的Flowchart
            var wf_Flowchart = this.ParseWF4Flowchart(workflowDefinition);
            //解析ActivitySettings
            var activitySettings = this.ParseActivitySettings(activitySettingsDefinition);
            this.ParseWorkflowActivity(wf_Flowchart, activitySettings, true);
            return activitySettings;
        }
        WorkflowActivity IWorkflowConverter.Parse(string workflowDefinition, IList<ActivitySetting> activitySettings)
        {
            //解析WF4的Flowchart
            var wf_Flowchart = this.ParseWF4Flowchart(workflowDefinition);
            return this.ParseWorkflowActivity(wf_Flowchart, activitySettings, false);
        }
        string IWorkflowConverter.Parse(WorkflowActivity activity, string originalWorkflowDefinition)
        {
            return null;
        }

        //解析Flowchart
        private System.Activities.Statements.Flowchart ParseWF4Flowchart(string xaml)
        {
            var bytes = Encoding.UTF8.GetBytes(xaml);

            System.Activities.Activity wf_Activity = null;
            try
            {
                using (var memoryStream = new MemoryStream(bytes))
                    wf_Activity = ActivityXamlServices.Load(memoryStream);
            }
            catch
            {
                throw new InvalidOperationException("无法加载Xaml字符串，请检查Xaml是否符合规则");
            }
            if (wf_Activity == null)
                throw new InvalidOperationException("Xaml字符串无法转换为WF4的活动树");

            if (!(wf_Activity is System.Activities.DynamicActivity))
                throw new InvalidOperationException("非DynamicActivity");

            var wf_dynamicActivity = wf_Activity as System.Activities.DynamicActivity;
            if (wf_dynamicActivity.Implementation == null)
                throw new InvalidOperationException("Implementation为空");

            var bodyActivity = wf_dynamicActivity.Implementation();
            if (!(bodyActivity is System.Activities.Statements.Flowchart))
                throw new InvalidOperationException("根活动请使用Flowchart");

            return bodyActivity as System.Activities.Statements.Flowchart;
        }
        //构建流程节点
        private void BuildFlowNode(System.Activities.Statements.FlowNode flowNode
            , Dictionary<int, FlowNodeInfo> flowNodes
            , List<FlowLineInfo> flowLines
            , int depth)
        {
            if (depth >= MAX_DEPTN)
                throw new Exception("超过递归最大深度");

            if (flowNode is System.Activities.Statements.FlowSwitch<string>)
            {
                var wf_FlowSwitch = flowNode as System.Activities.Statements.FlowSwitch<string>;
                var wf_default = wf_FlowSwitch.Default;
                if (wf_default != null)
                {
                    if (!flowLines.Exists(o => o.StartHashCode == wf_FlowSwitch.GetHashCode()
                            && o.EndHashCode == wf_default.GetHashCode()))
                    {
                        flowLines.Add(new FlowLineInfo()
                        {
                            StartHashCode = wf_FlowSwitch.GetHashCode(),
                            EndHashCode = wf_default.GetHashCode()
                        });
                        var flowSwitch = flowNodes[wf_FlowSwitch.GetHashCode()].FlowStep.Next as Taobao.Activities.Statements.FlowSwitch<string>;
                        flowSwitch.Default = flowNodes[wf_default.GetHashCode()].FlowStep;
                        BuildFlowNode(wf_default, flowNodes, flowLines, ++depth);
                    }
                }
                foreach (var wf_case in wf_FlowSwitch.Cases)
                {
                    if (!flowLines.Exists(o => o.StartHashCode == wf_FlowSwitch.GetHashCode()
                        && o.EndHashCode == wf_case.Value.GetHashCode()))
                    {
                        flowLines.Add(new FlowLineInfo()
                        {
                            StartHashCode = wf_FlowSwitch.GetHashCode(),
                            EndHashCode = wf_case.Value.GetHashCode()
                        });
                        var flowSwitch = flowNodes[wf_FlowSwitch.GetHashCode()].FlowStep.Next as Taobao.Activities.Statements.FlowSwitch<string>;
                        flowSwitch.Cases.Add(wf_case.Key, flowNodes[wf_case.Value.GetHashCode()].FlowStep);
                        BuildFlowNode(wf_case.Value, flowNodes, flowLines, ++depth);
                    }
                    else
                        continue;
                }
            }
            else if (flowNode is System.Activities.Statements.FlowStep)
            {
                var wf_flowNode = flowNode as System.Activities.Statements.FlowStep;
                var wf_nodeNext = wf_flowNode.Next;
                if (wf_nodeNext != null)
                {
                    flowNodes[flowNode.GetHashCode()].FlowStep.Next = flowNodes[wf_flowNode.Next.GetHashCode()].FlowStep;
                    BuildFlowNode(wf_nodeNext, flowNodes, flowLines, ++depth);
                }
            }
        }
        //设置变量集合
        private void SetVariablesForActivity(System.Collections.ObjectModel.Collection<System.Activities.Variable> variables
            , WorkflowActivity activity)
        {
            foreach (var variable in variables)
            {
                //判断变量名称是否合法
                if (Regex.IsMatch(variable.Name, @"[a-zA-Z][a-zA-Z_0-9]*"))
                {
                    if (variable.GetType().IsGenericType)
                    {
                        var dataType = variable.GetType().GetGenericArguments()[0];
                        if (dataType == typeof(String))
                            activity.Variables.Add(new Taobao.Activities.Variable<String>(variable.Name));
                        else if (dataType == typeof(Int32))
                            activity.Variables.Add(new Taobao.Activities.Variable<Int32>(variable.Name));
                        else if (dataType == typeof(Int64))
                            activity.Variables.Add(new Taobao.Activities.Variable<Int64>(variable.Name));
                    }
                }
            }
        }
        //通过Flowchart节点和自定义活动列表解析NTFE-BPM活动树
        private Statements.WorkflowActivity ParseWorkflowActivity(System.Activities.Statements.Flowchart flowchart
          , IList<ActivitySetting> activitySettings
          , bool isCacheMetaData)
        {
            var activity = new WorkflowActivity();
            //设置变量集合
            this.SetVariablesForActivity(flowchart.Variables, activity);

            var flowNodes = new Dictionary<int, FlowNodeInfo>(); //定义点集合
            var flowLines = new List<FlowLineInfo>(); //定义线段集合

            var temp = new List<string>();
            foreach (var wf_node in flowchart.Nodes)
            {
                if (wf_node is System.Activities.Statements.FlowSwitch<string>)
                {
                    var wf_FlowSwitch = wf_node as System.Activities.Statements.FlowSwitch<string>;
                    var literal = (System.Activities.Expressions.Literal<string>)wf_FlowSwitch.Expression;
                    if (literal == null || literal.Value == string.Empty)
                        throw new Exception(@"流程中包括名称为空的节点，请检查！");
                    var expression = literal.Value;
                    var activityName = ParseActivityName(expression);
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
                        //创建自动节点
                        var resultTo = activity.Variables.ToList().FirstOrDefault(o => o.Name == serverSetting.ResultTo);
                        flowStep = WorkflowBuilder.CreateServer(activitySetting
                               , activitySetting.ActivityName
                               , serverSetting.Script
                               , customSetting.FinishRule.Scripts
                               , resultTo != null ? (Variable<string>)resultTo : null
                               , activity.CustomActivityResult
                               , new Dictionary<string, Taobao.Activities.Statements.FlowNode>()
                               , null);
                    }
                    flowNodes.Add(wf_FlowSwitch.GetHashCode(), new FlowNodeInfo() { WF_FlowNode = wf_FlowSwitch, FlowStep = flowStep });
                }
                else if (wf_node is System.Activities.Statements.FlowStep)
                {
                    var wf_FlowStep = wf_node as System.Activities.Statements.FlowStep;
                    if (typeof(System.Activities.Statements.Parallel).IsAssignableFrom(wf_FlowStep.Action.GetType()))
                    {
                        IList<Custom> customs = new List<Custom>();
                        var wf_Parallel = wf_FlowStep.Action as System.Activities.Statements.Parallel;

                        foreach (var wf_Activity in wf_Parallel.Branches)
                        {
                            if (typeof(ICustom).IsAssignableFrom(wf_Activity.GetType()))
                            {
                                var expression = (wf_Activity as ICustom).DisplayName;
                                if (string.IsNullOrEmpty(expression))
                                    throw new Exception(@"流程中包括名称为空的节点，请检查！");
                                var activityName = ParseActivityName(expression);
                                if (temp.Contains(activityName))
                                    throw new InvalidOperationException("流程中包括重复名称的节点，请检查！");
                                temp.Add(activityName);
                                var activitySetting = activitySettings.FirstOrDefault(o => o.ActivityName == activityName);
                                if (activitySetting == null)
                                    throw new Exception(string.Format(@"解析出错，找不到名称为“{0}”的ActivitySetting", activityName));
                                if (activitySetting is HumanSetting)
                                {
                                    var humanSetting = activitySetting as HumanSetting;
                                    var human = WorkflowBuilder.CreateHuman(activitySetting
                                        , activitySetting.ActivityName
                                        , new GetUsers(humanSetting.ActionerRule.Scripts)
                                        , activity.CustomActivityResult);
                                    customs.Add(human);
                                }
                                else if (activitySetting is ServerSetting)
                                {
                                    var customSetting = activitySetting as CustomSetting;
                                    var serverSetting = activitySetting as ServerSetting;
                                    var resultTo = activity.Variables.ToList().FirstOrDefault(o => o.Name == serverSetting.ResultTo);
                                    var server = WorkflowBuilder.CreateServer(activitySetting
                                        , activitySetting.ActivityName
                                        , serverSetting.Script
                                        , customSetting.FinishRule.Scripts
                                        , resultTo != null ? (Variable<string>)resultTo : null
                                        , activity.CustomActivityResult);
                                    customs.Add(server);
                                }
                            }
                        }
                        var p_expression = wf_Parallel.DisplayName;
                        if (string.IsNullOrEmpty(p_expression))
                            throw new Exception(@"流程中包括名称为空的节点，请检查！");
                        var p_activityName = ParseActivityName(p_expression);
                        if (temp.Contains(p_activityName))
                            throw new InvalidOperationException("流程中包括重复名称的节点，请检查！");
                        temp.Add(p_activityName);
                        var p_activitySetting = activitySettings.FirstOrDefault(o => o.ActivityName == p_activityName);
                        if (p_activitySetting == null)
                            throw new Exception(string.Format(@"解析出错，找不到名称为“{0}”的ActivitySetting", p_activityName));
                        var flowStep = WorkflowBuilder.CreateParallel(p_activitySetting
                            , p_activitySetting.ActivityName
                            , (p_activitySetting as ParallelSetting).CompletionCondition
                            , null
                            , customs.ToArray());
                        flowNodes.Add(wf_FlowStep.GetHashCode(), new FlowNodeInfo() { WF_FlowNode = wf_FlowStep, FlowStep = flowStep });
                    }
                }
            }

            var flowNode = flowchart.StartNode;
            if (flowNode == null)
                throw new InvalidOperationException("不包括StartNode开始节点");
            //创建流程节点
            BuildFlowNode(flowNode, flowNodes, flowLines, 0);
            //将节点树连接到NTFE
            activity.Body.StartNode = flowNodes[flowNode.GetHashCode()].FlowStep;

            //初始化Flowchart节点的元素
            if (isCacheMetaData)
                CacheMetadata(activity.Body);

            return activity;
        }
        //通过自定义活动文本描述解析自定义活动列表 用于首次发布时解析
        private IList<ActivitySetting> ParseActivitySettings(string activitySettingsDefinition)
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

                bool isChildOfActivity;
                bool.TryParse(element.Attribute("isChildOfActivity") == null
                    ? "False" : element.Attribute("isChildOfActivity").Value, out isChildOfActivity);

                activitySettings.Add(new HumanSetting(WorkflowBuilder.Default_FlowNodeIndex//HACK:设置默认索引，用于临时标记
                    , activityName
                    , actions.ToArray()
                    , Convert.ToInt32(slotCount)
                    , slotMode.ToLower() == "oneatonce" ? HumanSetting.SlotDistributionMode.OneAtOnce : HumanSetting.SlotDistributionMode.AllAtOnce
                    , url
                    , startRule
                    , new HumanActionerRule(actionerRules)
                    , new FinishRule(scripts)
                    , null
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
        //初始化元数据
        private void CacheMetadata(Taobao.Activities.Statements.Flowchart flowchart)
        {
            var methodInfo = flowchart.GetType().GetMethod("CacheMetadata", BindingFlags.Instance
                | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly);
            methodInfo.Invoke(flowchart, null);
        }
        //获取节点名称
        private string ParseActivityName(string expression)
        {
            var match = Regex.Match(expression, @"(.+?)->.*");
            if (match.Success)
                expression = match.Groups[1].Value;
            return expression;
        }
        //过滤表达式
        private string ReplaceExpression(string input)
        {
            var match = Regex.Match(input, @"(.+?)->.*");
            if (match.Success)
                input = match.Groups[1].Value;
            return input;
        }

        #region 相关子类
        /// <summary>
        /// 活动节点信息
        /// </summary>
        class FlowNodeInfo
        {
            /// <summary>
            /// WF4的FlowSwitch<string>
            /// </summary>
            public System.Activities.Statements.FlowNode WF_FlowNode { get; set; }
            /// <summary>
            /// 对应的NTFE的活动节点
            /// </summary>
            public Taobao.Activities.Statements.FlowStep FlowStep { get; set; }
        }
        /// <summary>
        /// 活动路线信息
        /// </summary>
        class FlowLineInfo
        {
            /// <summary>
            /// 起始Hash值
            /// </summary>
            public int StartHashCode { get; set; }
            /// <summary>
            /// 结束Hash值
            /// </summary>
            public int EndHashCode { get; set; }
        }
        #endregion
    }
}
