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
using NUnit.Framework;

namespace Taobao.Workflow.Activities.Test
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class ProcessTypeTest : BaseTest
    {
        public static readonly string TYPENAME = "test";
        public static readonly string ActivityName = "审批";
        private static readonly string[] _actions = new string[] { "同意", "否决" };

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Save()
        {
            var type = new ProcessType(TYPENAME
                , new ProcessType.WorkflowDefinition("")
                , Taobao.Workflow.Activities.Test.Stub.WorkflowParser.GetActivitySettings().ToArray()) { Group = "test" };
            this._processTypeService.Create(type);
            Assert.AreNotEqual(Guid.Empty, type.ID);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Get()
        {
            this.CreateProcessType(TYPENAME);
            var type = this._processTypeService.GetProcessType(TYPENAME);
            var histories = this._processTypeService.GetHistories(TYPENAME);
            var setting = type.GetHumanSetting("节点1");
            Assert.AreEqual(TYPENAME, type.Name);
            Assert.AreEqual(Group, type.Group);
            Assert.IsNotNull(setting);
            Assert.AreEqual(0, setting.FlowNodeIndex);
            Assert.AreEqual(string.Join("$", _actions), string.Join("$", setting.Actions));
            Assert.IsTrue(type.IsCurrent);
            Assert.IsTrue(!histories.Contains(type));
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void GetByVersion()
        {
            this.CreateProcessType(TYPENAME);
            var type = this._processTypeService.GetProcessType(TYPENAME);
            _log.Info(this._processTypeService.GetProcessType(TYPENAME, type.Version));
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void SetAsCurrent()
        {
            var name = TYPENAME + DateTime.Now;
            this.CreateProcessType(name);
            //版本的精度到秒
            System.Threading.Thread.Sleep(2000);
            this.CreateProcessType(name);

            var target = this._processTypeService.GetHistories(name).First();
            this._processTypeService.SetAsCurrent(target.Name, target.Version);

            var current = this._processTypeService.GetProcessType(target.Name);
            Assert.AreEqual(target.Version, current.Version);
            Assert.AreEqual(target.ID, current.ID);
            Assert.AreEqual(target, current);
        }
    }
}
