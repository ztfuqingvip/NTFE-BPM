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
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Taobao.Workflow.Activities.Application;
using Castle.MicroKernel.Registration;
using CodeSharp.Core.Services;

namespace Taobao.Workflow.Activities.Test
{
    [TestClass]
    public class WorkflowParserTest : BaseTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var parser = DependencyResolver.Resolve<IWorkflowParser>();
            var p = parser.Parse(this.GetActivitySettings());
        }

        protected override void Resolve(Castle.Windsor.IWindsorContainer windsor)
        {
            //优先注册测试桩
            windsor.Register(Component.For<IWorkflowParser>().ImplementedBy<WorkflowParser>());
            base.Resolve(windsor);
        }

        public IEnumerable<ActivitySetting> GetActivitySettings()
        {
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
                , new HumanActionerRule("originator", "getSuperior()", "username1")
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
    }
}
