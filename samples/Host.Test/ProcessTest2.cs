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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities.Client;
using NUnit.Framework;

namespace Host.Test
{
    [TestFixture]
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class ProcessTest2 : BaseTest
    {
        private string _processType = "EngineClientTest2";
        private string _node1 = "节点1";
        private string _node2 = "节点2";
        private string _node3 = "节点3";
        private string _variable1 = "username1";

        //用于测试的流程 变量username1
        //节点1 发起人/主管 同意|否决 all('同意')|atMostOf(1,'否决') 同意->节点2，否决->节点1（自身）
        //节点2 自动将主管（getSuperior()）赋给username1变量 ->节点3
        //节点3 由username1归档 完成

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_1_2_3()
        {
            var engine = this._clientApi;

            //发起流程
            var p = this.CreateProcess();
            _log.Info(p.ID);
            this.Idle();

            _log.Info("节点1处理...");
            #region 节点1 同意
            //主管
            var ws = engine.GetWorkItemsByProcess(_superior, p.ID, null);
            //列表
            var w = this.GetAndAssertWorkItem(ws, _node1);
            //get
            engine.GetWorkItem(w.ID, _superior);
            //open
            engine.OpenWorkItem(w.ID, _superior);
            Assert.AreEqual(WorkItemStatus.Open, engine.GetWorkItem(w.ID, _superior).Status);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _superior, "同意", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _superior));

            //发起人
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node1);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _originator, "同意", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            _log.Info("节点2自动处理（将主管（getSuperior()）赋给username1变量）...");

            _log.Info("节点3处理...");
            #region 节点3 完成
        
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_superior, p.ID, null), _node3);
            //execute 完成
            engine.ExecuteWorkItem(w.ID, _superior, "完成", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _superior));

            this.Idle();
            #endregion

            //流程结束
            Assert.AreEqual(ProcessStatus.Completed, engine.GetProcess(p.ID).Status);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_1_1_2()
        {
            var engine = this._clientApi;

            //发起流程
            var p = this.CreateProcess();
            _log.Info(p.ID);
            this.Idle();

            _log.Info("节点1处理...");
            #region 节点1 否决则退回到自身节点
            //主管
            var ws = engine.GetWorkItemsByProcess(_superior, p.ID, null);
            //列表
            var w = this.GetAndAssertWorkItem(ws, _node1);
            //get
            engine.GetWorkItem(w.ID, _superior);
            //open
            engine.OpenWorkItem(w.ID, _superior);
            Assert.AreEqual(WorkItemStatus.Open, engine.GetWorkItem(w.ID, _superior).Status);
            //execute 否决
            engine.ExecuteWorkItem(w.ID, _superior, "否决", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _superior));

            this.Idle();
            #endregion

            _log.Info("节点1第二次处理...");
            #region 节点1 同意
            //主管
            ws = engine.GetWorkItemsByProcess(_superior, p.ID, null);
            //列表
            w = this.GetAndAssertWorkItem(ws, _node1);
            //get
            engine.GetWorkItem(w.ID, _superior);
            //open
            engine.OpenWorkItem(w.ID, _superior);
            Assert.AreEqual(WorkItemStatus.Open, engine.GetWorkItem(w.ID, _superior).Status);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _superior, "同意", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _superior));

            //发起人
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node1);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _originator, "同意", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            _log.Info("节点2自动处理（将主管（getSuperior()）赋给username1变量）...");
        }

        private void CreateProcessType()
        {
            this.CreateProcessType(this._processType, Resource.Workflow2, Resource.Settings2);
        }
        private Process CreateProcess()
        {
            this.CreateProcessType();
            return this.CreateProcess(_processType, new Dictionary<string, string>() { { _variable1, this._originator } });
        }
        private WorkItem GetAndAssertWorkItem(WorkItem[] ws, string activityName)
        {
            Assert.AreEqual(1, ws.Length);
            var w = ws.First();
            Assert.IsNotNull(w);
            Assert.AreEqual(activityName, w.ActivityName);
            return w;
        }
    }
}
