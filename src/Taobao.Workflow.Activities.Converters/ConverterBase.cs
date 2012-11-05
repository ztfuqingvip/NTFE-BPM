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
using System.Xml.Linq;

namespace Taobao.Workflow.Activities.Converters
{
    public abstract class ConverterBase
    {
        /// <summary>
        /// 将ActivitySettings转换为Settings格式
        /// </summary>
        /// <param name="activitySettings"></param>
        /// <returns></returns>
        public string ParseActivitySettingsDefinition(IList<ActivitySetting> activitySettings)
        {
            var elements = new XElement("activities");
            activitySettings.ToList().ForEach(o =>
            {
                if (o is HumanSetting)
                {
                    var humanSetting = o as HumanSetting;
                    var actionItems = new List<XElement>();
                    humanSetting.Actions.ToList().ForEach(e => { actionItems.Add(new XElement("action", new XCData(e))); });
                    var actionerItems = new List<XElement>();
                    humanSetting.ActionerRule.Scripts.ToList().ForEach(e => { actionerItems.Add(new XElement("actioner", new XCData(e))); });

                    var scriptItems = this.ParseFinishRuleDefinition(humanSetting);
                    XElement startRule = this.ParseStartRuleDefinition(humanSetting);

                    //解析EscalationRule
                    XElement escalationRule;
                    if (humanSetting.EscalationRule == null)
                        escalationRule = new XElement("escalation"
                            , new XAttribute("expiration", "")
                            , new XAttribute("repeat", "")
                            , new XAttribute("notifyTemplateName", "")
                            , new XAttribute("gotoActivityName", "")
                            , new XAttribute("redirectTo", "")
                            , new XAttribute("zone", ""));
                    else
                        escalationRule = new XElement("escalation"
                            , new XAttribute("expiration", humanSetting.EscalationRule.ExpirationMinutes.HasValue ? humanSetting.EscalationRule.ExpirationMinutes.Value.ToString() : "")
                            , new XAttribute("repeat", humanSetting.EscalationRule.NotifyRepeatMinutes.HasValue ? humanSetting.EscalationRule.NotifyRepeatMinutes.Value.ToString() : "")
                            , new XAttribute("notifyTemplateName", !string.IsNullOrEmpty(humanSetting.EscalationRule.NotifyTemplateName) ? humanSetting.EscalationRule.NotifyTemplateName : "")
                            , new XAttribute("gotoActivityName", !string.IsNullOrEmpty(humanSetting.EscalationRule.GotoActivityName) ? humanSetting.EscalationRule.GotoActivityName : "")
                            , new XAttribute("redirectTo", !string.IsNullOrEmpty(humanSetting.EscalationRule.RedirectTo) ? humanSetting.EscalationRule.RedirectTo : "")
                            , new XAttribute("zone", humanSetting.EscalationRule.TimeZone != null ? humanSetting.EscalationRule.TimeZone.Name : ""));

                    var element = new XElement("human"
                        , new XAttribute("name", humanSetting.ActivityName)
                        , new XAttribute("isChildOfActivity", humanSetting.IsChildOfActivity)
                        , new XAttribute("slot", humanSetting.SlotCount)
                        , new XAttribute("slotMode", humanSetting.SlotMode == HumanSetting.SlotDistributionMode.AllAtOnce
                            ? "allAtOnce" : "oneAtOnce")
                        , new XAttribute("url", humanSetting.Url)
                        , new XElement("actions", actionItems)
                        , startRule
                        , new XElement("actioners", actionerItems)
                        , new XElement("finish", scriptItems)
                        , escalationRule);
                    elements.Add(element);
                }
                else if (o is ServerSetting)
                {
                    var serverSetting = o as ServerSetting;

                    var scriptItems = this.ParseFinishRuleDefinition(serverSetting);
                    XElement startRule = this.ParseStartRuleDefinition(serverSetting);

                    var element = new XElement("server"
                        , new XAttribute("name", serverSetting.ActivityName)
                        , new XAttribute("isChildOfActivity", serverSetting.IsChildOfActivity)
                        , new XAttribute("resultTo", serverSetting.ResultTo)
                        , startRule
                        , new XElement("script", new XCData(serverSetting.Script))
                        , new XElement("finish", scriptItems));
                    elements.Add(element);
                }
                else if (o is SubProcessSetting)
                {
                    var subProcessSetting = o as SubProcessSetting;

                    var scriptItems = this.ParseFinishRuleDefinition(subProcessSetting);
                    XElement startRule = this.ParseStartRuleDefinition(subProcessSetting);

                    var element = new XElement("subProcess"
                        , new XAttribute("name", subProcessSetting.ActivityName)
                        , startRule
                        , new XElement("finish", scriptItems)
                        , new XAttribute("isChildOfActivity", subProcessSetting.IsChildOfActivity)
                        , new XAttribute("processTypeName", subProcessSetting.SubProcessTypeName));
                    elements.Add(element);
                }
                else if (o is ParallelSetting)
                {
                    var parallelSetting = o as ParallelSetting;

                    var element = new XElement("parallel"
                        , new XAttribute("name", parallelSetting.ActivityName)
                        , new XAttribute("isChildOfActivity", parallelSetting.IsChildOfActivity)
                        , new XElement("condition", new XCData(parallelSetting.CompletionCondition)));
                    elements.Add(element);
                }
            });
            return elements.ToString();
        }

        //解析完成规则
        private List<XElement> ParseFinishRuleDefinition(CustomSetting customSetting)
        {
            var scriptItems = new List<XElement>();
            if(customSetting.FinishRule != null)
                customSetting.FinishRule.Scripts.ToList().ForEach(e =>
                {
                    scriptItems.Add(new XElement("line", new XAttribute("name", e.Key), new XCData(e.Value)));
                });
            return scriptItems;
        }
        //解析开始规则
        private XElement ParseStartRuleDefinition(CustomSetting customSetting)
        {
            XElement startRule;
            if (customSetting.StartRule == null)
                startRule = new XElement("start", new XAttribute("at", ""), new XAttribute("after", ""), new XAttribute("zone", ""));
            else
                startRule = new XElement("start"
                , new XAttribute("at", customSetting.StartRule.At != null ? customSetting.StartRule.At.ToString() : string.Empty)
                , new XAttribute("after", customSetting.StartRule.AfterMinutes.HasValue ? customSetting.StartRule.AfterMinutes.ToString() : string.Empty)
                , new XAttribute("zone", customSetting.StartRule.TimeZone != null ? customSetting.StartRule.TimeZone.Name : string.Empty));
            return startRule;
        }
    }
}
