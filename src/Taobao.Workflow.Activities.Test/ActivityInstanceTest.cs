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
    /// <summary>
    /// 各类型节点实例逻辑测试
    /// </summary>
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class ActivityInstanceTest : BaseTest
    {
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Human()
        {
            var instance = new HumanActivityInstance(Guid.NewGuid(), 0, 1, "人工节点", "bookmark", new string[] { "houkun", "xiaoxuan" });
            this._processService.CreateActivityInstance(instance);
            Assert.AreNotEqual(0, instance.ID);
            //TODO:对私有方法进行断言
            this.Evict(instance);

            var instance2 = this._processService.GetActivityInstance(instance.ID);
            Assert.IsNotNull(instance2);
            Assert.IsInstanceOf<HumanActivityInstance>(instance2);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Server()
        {
            var instance = new ServerActivityInstance(Guid.NewGuid(), 0, 1, "人工节点");
            this._processService.CreateActivityInstance(instance);
            Assert.AreNotEqual(0, instance.ID);
            this.Evict(instance);

            var instance2 = this._processService.GetActivityInstance(instance.ID);
            Assert.IsNotNull(instance2);
            Assert.IsInstanceOf<ServerActivityInstance>(instance2);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void SubProcess()
        {
            var instance = new SubProcessActivityInstance(Guid.NewGuid(), 0, 1, "人工节点", "bookmark");
            this._processService.CreateActivityInstance(instance);
            Assert.AreNotEqual(0, instance.ID);
            this.Evict(instance);

            var instance2 = this._processService.GetActivityInstance(instance.ID);
            Assert.IsNotNull(instance2);
            Assert.IsInstanceOf<SubProcessActivityInstance>(instance2);
        }

        [Test(Description = "节点实例获取测试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void GetActivityInstanceByWorkflowActivityInstanceId()
        {
            var process = this.CreateProcess();
            var instance = new HumanActivityInstance(process.ID, 0, 1, "人工节点", "bookmark", new string[] { "houkun", "xiaoxuan" });
            this._processService.CreateActivityInstance(instance);

            var instance2 = this._processService.GetActivityInstanceByWorkflowActivityInstanceId(process, 1);
            Assert.IsNotNull(instance2);
            Assert.IsInstanceOf<HumanActivityInstance>(instance2);
            Assert.AreEqual(instance.ID, instance2.ID);
        }
        
        [Test(Description = "子流程节点实例获取测试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void GetSubProcessActivityInstances()
        {
            var process1 = this.CreateProcess();
            var process2 = this.CreateProcess();
            var instance = new SubProcessActivityInstance(process1.ID, 0, 1, "人工节点", "bookmark", process2.ID);
            this._processService.CreateActivityInstance(instance);
            var instance2 = this._processService.GetSubProcessActivityInstances(process1, process2);
            Assert.IsNotNull(instance2);
            Assert.AreEqual(instance.ID, instance2.ID);
        }
    }
}