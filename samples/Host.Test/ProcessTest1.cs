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
    public class ProcessTest1 : BaseTest
    {
        private string _processType = "EngineClientTest1";
        private string _node1 = "节点1";
        private string _node2 = "节点2";
        private string _node3 = "节点3";
        private string _node4 = "节点4";
        private string _variable1 = "username1";

        //用于测试的流程 变量username1
        //节点1 发起人 同意|否决 all('同意')|all('否决') 同意->节点2 否决->节点3
        //节点2 主管 同意|否决 all('同意')|all('否决') 同意->节点4 否决->节点3
        //节点3 由username1归档 完成
        //节点4 由username1归档 完成

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_1_2_4()
        {
            var engine = this._clientApi;

            //发起流程
            var p = this.CreateProcess();
            _log.Info(p.ID);
            this.Idle();

            _log.Info("节点1处理...");
            #region 节点1 同意
            //发起人
            var ws = engine.GetWorkItemsByProcess(_originator, p.ID, null);
            //列表
            var w = this.GetAndAssertWorkItem(ws, _node1);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _originator, "同意", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            _log.Info("节点2处理...");
            #region 节点2 同意
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_superior, p.ID, null), _node2);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _superior, "同意", new Dictionary<string, string>() { { _variable1, _originator } });
            Assert.IsNull(engine.GetWorkItem(w.ID, _superior));

            this.Idle();
            #endregion

            _log.Info("节点4处理...");
            #region 节点4 完成
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node4);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _originator, "完成", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            //流程结束
            Assert.AreEqual(ProcessStatus.Completed, engine.GetProcess(p.ID).Status);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_1_2_3()
        {
            var engine = this._clientApi;
            //发起流程
            var p = this.CreateProcess();
            this.Idle();

            _log.Info("节点1处理...");
            #region 节点1 同意
            //发起人
            var ws = engine.GetWorkItemsByProcess(_originator, p.ID, null);
            //列表
            var w = this.GetAndAssertWorkItem(ws, _node1);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _originator, "同意", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            _log.Info("节点2处理...");
            #region 节点2 否决
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_superior, p.ID, null), _node2);
            //execute 否决
            engine.ExecuteWorkItem(w.ID, _superior, "否决", new Dictionary<string, string>() { { _variable1, _originator } });
            Assert.IsNull(engine.GetWorkItem(w.ID, _superior));

            this.Idle();
            #endregion

            _log.Info("节点3处理...");
            #region 节点3 完成
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node3);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _originator, "完成", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            //流程结束
            Assert.AreEqual(ProcessStatus.Completed, engine.GetProcess(p.ID).Status);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_1_3()
        {
            var engine = this._clientApi;
            //发起流程
            var p = this.CreateProcess();
            this.Idle();

            _log.Info("节点1处理...");
            #region 节点1 同意
            //发起人
            var ws = engine.GetWorkItemsByProcess(_originator, p.ID, null);
            //列表
            var w = this.GetAndAssertWorkItem(ws, _node1);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _originator, "否决", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            _log.Info("节点3处理...");
            #region 节点3 完成
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node3);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _originator, "完成", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            //流程结束
            Assert.AreEqual(ProcessStatus.Completed, engine.GetProcess(p.ID).Status);
        }

        private void CreateProcessType()
        {
            this.CreateProcessType(this._processType, Resource.Workflow1, Resource.Settings1);
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
