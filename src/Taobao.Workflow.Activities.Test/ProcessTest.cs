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
using System.Threading;
using NUnit.Framework;
using Taobao.Workflow.Activities.Hosting;
using CodeSharp.Core.Castles;

namespace Taobao.Workflow.Activities.Test
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class ProcessTest : BaseTest
    {
        [Test(Description = "创建流程验证持久化正确性")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Create()
        {
            var process = this.CreateProcess();
            this.Evict(process);
            var fromdb = this._processService.GetProcess(process.ID);
            Assert.IsFalse(fromdb.FinishTime.HasValue);
            Assert.IsNotNull(fromdb.Originator);
            Assert.IsNotNull(fromdb.ProcessType);
            Assert.AreEqual(fromdb.ProcessType.Name, process.ProcessType.Name);
            Assert.AreEqual(0, TestHelper.GetCurrentNode(fromdb));
        }
        [Test(Description = "验证内置变量处理的严谨性")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void UpdateDataFields()
        {
            var p = new Process("Test"
                , this.CreateProcessType(UnitTestType)
                , this.GetUser("houkun")
                , 0
                , new Dictionary<string, string>() { 
                    { "key1", "test" }
                });
            //不允许公开获取内置变量
            Assert.IsFalse(p.GetDataFields().ContainsKey(WorkflowBuilder.Variable_CurrentNode));
            //应插入默认的当前节点索引
            Assert.AreEqual(0, TestHelper.GetCurrentNode(p));
            Assert.AreEqual("test", p.GetDataFields()["key1"]);

            p = new Process("Test"
                , this.CreateProcessType(UnitTestType)
                , this.GetUser("houkun")
                , 0
                , new Dictionary<string, string>() { 
                    { WorkflowBuilder.Variable_CurrentNode, "10" },
                    { "key1", "test" }
                });
            //不允许公开获取内置变量
            Assert.IsFalse(p.GetDataFields().ContainsKey(WorkflowBuilder.Variable_CurrentNode));
            //不允许外部更新当前节点索引
            Assert.AreEqual(0, TestHelper.GetCurrentNode(p));
            Assert.AreEqual("test", p.GetDataFields()["key1"]);

            p.UpdateDataField(WorkflowBuilder.Variable_CurrentNode, "10");
            //不允许公开获取内置变量
            Assert.IsFalse(p.GetDataFields().ContainsKey(WorkflowBuilder.Variable_CurrentNode));
            //不允许外部更新当前节点索引
            Assert.AreEqual(0, TestHelper.GetCurrentNode(p));

            p.UpdateDataField("key1", "test123");
            Assert.AreEqual("test123", p.GetDataFields()["key1"]);

            p.UpdateDataFields(new Dictionary<string, string>() { 
                { WorkflowBuilder.Variable_CurrentNode, "10" },
                { "key1", "test456" }
            });
            //不允许公开获取内置变量
            Assert.IsFalse(p.GetDataFields().ContainsKey(WorkflowBuilder.Variable_CurrentNode));
            //不允许外部更新当前节点索引
            Assert.AreEqual(0, TestHelper.GetCurrentNode(p));
            Assert.AreEqual("test456", p.GetDataFields()["key1"]);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Search()
        {
            Assert.AreEqual(0, this._managementApi.SearchProcesses(new Management.ProcessQuery(), 1, 20).Total);

            _log.Info("搜索：" + this._managementApi.SearchProcesses(new Management.ProcessQuery()
            {
                ProcessTypeName = UnitTestType,
                Status = new Client.ProcessStatus[] { Client.ProcessStatus.Active },
                //Originator = "A2283BCD-6AB1-401E-ABB1-9F8300EE29D6",
                Title = "1",
                //CreateTo = DateTime.Now,
                CreateFrom = DateTime.Now
            }, 1, 20).Total);

            _log.Info("搜索Fault：" + this._managementApi.SearchProcesses(new Management.ProcessQuery()
            {
                Status = new Client.ProcessStatus[] { Client.ProcessStatus.Error },
            }, 1, 20).Total);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Update()
        {
            var process = this.CreateProcess();
            process.UpdateDataField("key", "123");
            this._processService.Update(process);
            this.Evict(process);
            process = this._processService.GetProcess(process.ID);
            Assert.AreEqual("123", process.GetDataFields()["key"]);
        }

        //以下操作都必须在流程处理调度安全状态

        [Test(Description = "切换流程版本，切换后仍应从相同节点开始运行")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void ChangeProcessType()
        {
            var process = this.CreateProcess();
            //由于版本精度在秒
            Thread.Sleep(1000);
            //目标版本
            var targetVersion = this.CreateProcessType(UnitTestType);
            //创建一个错误的版本
            var wrongVersion = this.CreateProcessType("WrongType");

            //Error才允许变更
            try { this._processService.ChangeProcessType(process, targetVersion); Assert.IsTrue(false); }
            catch (Exception e) { _log.Info(e.Message); }

            //置为Error
            TestHelper.MarkProcessAsError(process);
            //只允许变更到不同的版本
            try { this._processService.ChangeProcessType(process, process.ProcessType); Assert.IsTrue(false); }
            catch (Exception e) { _log.Info(e.Message); }
            //只允许变更到同一流程的不同版本
            try { this._processService.ChangeProcessType(process, wrongVersion); Assert.IsTrue(false); }
            catch (Exception e) { _log.Info(e.Message); }

            //先将流程跳转到Node2
            this._processService.Goto(process, Node2);
            Assert.AreEqual(Node2Index, TestHelper.GetCurrentNode(process));
            //置为Error
            TestHelper.MarkProcessAsError(process);
            //切换版本
            this._processService.ChangeProcessType(process, targetVersion);
            //切换后仍应处于当前节点
            Assert.AreEqual(Node2Index, TestHelper.GetCurrentNode(process));
            this.AssertProcessOnlyStartRuntimeData(process);

            this.SyncScheduleUntil(process);

            //简单验证从Node2继续运行
            Assert.AreEqual(Node2Index, TestHelper.GetCurrentNode(process));
        }

        [Test(Description = "流程跳转，必须在流程安全状态下进行")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Goto()
        {
            var process = this.CreateProcess();
            //不能处于Running
            TestHelper.ChangeProcessStatus(process, ProcessStatus.Running);
            try { this._processService.Goto(process, Node2); Assert.IsTrue(false); }
            catch (Exception e) { _log.Info(e.Message); }

            TestHelper.MarkProcessAsActive(process);
            this._processService.Goto(process, Node2);
            Assert.AreEqual(Node2Index, TestHelper.GetCurrentNode(process));
            this.AssertProcessOnlyStartRuntimeData(process);
        }

        [Test(Description = "对处于Error的流程在当前位置进行重试")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Retry()
        {
            var process = this.CreateProcess();
            TestHelper.MarkProcessAsActive(process);
            //先将流程跳转到Node2
            this._processService.Goto(process, Node2);
            Assert.AreEqual(Node2Index, TestHelper.GetCurrentNode(process));

            //置为Error并重试
            TestHelper.MarkProcessAsError(process);
            this._processService.Retry(process);
            //从当前节点重试
            Assert.AreEqual(Node2Index, TestHelper.GetCurrentNode(process));
            //仅含有一条错误重试调度项
            this.AssertProcessOnlyOneRuntimeData<ErrorResumption>(process);

            //将调度项置为Error
            var r = this._resumptionService.GetValidWaitingResumptions(process).First();
            r.SetError(true);
            this._resumptionService.Update(r);
            //增加ErrorRecord
            this._resumptionService.AddErrorRecord(new FaultResumptionRecord(process, new Exception(), r.ID));
            this._resumptionService.AddErrorRecord(new FaultBookmarkRecord(process, new Exception(), "errorBookmark", Node2));
            //再次重试
            this._processService.Retry(process);
            var list = this._resumptionService.GetValidWaitingResumptions(process);
            //产生一条新的恢复调度项
            Assert.AreEqual(2, list.Count());
            //都不为Error
            Assert.IsFalse(list.ElementAt(0).IsError);
            Assert.IsFalse(list.ElementAt(1).IsError);
        }

        [Test(Description = "停止和重启流程")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void StopAndRestart()
        {
            var process = this.CreateProcess();
            TestHelper.MarkProcessAsActive(process);
            //先将流程跳转到Node2
            this._processService.Goto(process, Node2);
            Assert.AreEqual(Node2Index, TestHelper.GetCurrentNode(process));

            //运行调度以产生调度项
            this.SyncScheduleUntil(process);

            process = this._processService.GetProcess(process.ID);
            TestHelper.MarkProcessAsActive(process);
            this._processService.Stop(process);
            //在当前位置停止
            Assert.AreEqual(Node2Index, TestHelper.GetCurrentNode(process));
            //停止后不存在任何运行时数据
            this.AssertProcessDonotHaveRuntimeData(process);

            this._processService.Restart(process);
            //从当前节点重启
            Assert.AreEqual(Node2Index, TestHelper.GetCurrentNode(process));
            this.AssertProcessOnlyStartRuntimeData(process);
        }

        [Test(Description = "删除流程，验证与子流程相关逻辑")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Delete()
        {
            //父流程
            var process = this.CreateProcess();
            //子流程
            var subProcess = new Process("SubProcess From UnitTest at " + DateTime.Now
                , this.CreateProcessType(UnitTestType)
                , this.GetUser("houkun")
                , 0
                , null
                , process);
            this._processService.Create(subProcess);

            //running状态不能删除
            try { this._processService.Delete(process); Assert.IsTrue(false); }
            catch (Exception e) { _log.Info(e.Message); }
            //不能删除子流程
            TestHelper.MarkProcessAsActive(subProcess);
            try { this._processService.Delete(subProcess); Assert.IsTrue(false); }
            catch (Exception e) { _log.Info(e.Message); }

            this.SyncScheduleUntil(process);
            this.SyncScheduleUntil(subProcess);

            process = this._processService.GetProcess(process.ID);
            subProcess = this._processService.GetProcess(subProcess.ID);
            this._processService.Delete(process);
            //应置为deleted
            Assert.AreEqual(ProcessStatus.Deleted, process.Status);
            //没有运行时数据
            this.AssertProcessDonotHaveRuntimeData(process);
            //子流程也删除
            Assert.AreEqual(ProcessStatus.Deleted, subProcess.Status);
            this.AssertProcessDonotHaveRuntimeData(subProcess);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Rollback()
        {
            var user = this._userService.GetUserWhatever("houkun");
            var process = this.CreateProcess(user.UserName);
            this.SyncScheduleUntil(process);

            ActivityInstanceBase i;
            string reason;

            //首个节点不可回滚
            Assert.IsFalse(this._processService.CanRollback(process, user, out i, out reason));
            _log.Info(reason);

            //处理节点1
            Assert.AreEqual(Node1Index, TestHelper.GetCurrentNode(process));
            var w = this._workItemService.GetWorkItems(user, process, null).First();
            this._workItemService.Execute(w.ID, user, "同意", null);

            this.SyncScheduleUntil(process);
            //等待延迟调度完成
            Thread.Sleep(20000);
            this.SyncScheduleUntil(process);

            process = this._processService.GetProcess(process.ID);
            //此时在节点2
            Assert.AreEqual(Node2Index, TestHelper.GetCurrentNode(process));
            //是否允许回滚
            var result = this._processService.CanRollback(process, user, out i, out reason);
            _log.Info(reason);
            Assert.IsTrue(result);
            //上一个节点应为节点1
            Assert.AreEqual(Node1, i.ActivityName);

            this.Evict(process);
            process = this._processService.GetProcess(process.ID);
            //回滚到节点1
            this._processService.Rollback(process, user);

            this.SyncScheduleUntil(process);
            //回滚到节点1后
            Assert.AreEqual(Node1Index, TestHelper.GetCurrentNode(process));
        }

        //断言流程没有运行时数据
        private void AssertProcessDonotHaveRuntimeData(Process process)
        {
            //未完成的调度项
            Assert.AreEqual(0, this._resumptionService.GetValidWaitingResumptions(process).Count());
            //未完成的任务
            Assert.AreEqual(0, this._workItemService.GetWorkItems(process).Count());
        }
        //断言流程只含有一条流程启动调度
        private void AssertProcessOnlyStartRuntimeData(Process process)
        {
            AssertProcessOnlyOneRuntimeData<ProcessStartResumption>(process);
        }
        //断言流程只含有一条调度
        private void AssertProcessOnlyOneRuntimeData<T>(Process process)
        {
            //未完成的任务
            Assert.AreEqual(0, this._workItemService.GetWorkItems(process).Count());
            var list = this._resumptionService.GetValidWaitingResumptions(process);
            //只有1条流程启动调度项
            Assert.AreEqual(1, list.Count());
            Assert.IsInstanceOf<T>(list.First());
        }
    }
}