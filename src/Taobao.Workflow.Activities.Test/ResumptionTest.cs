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
using System.Threading;
using NHibernate.Criterion;
using Taobao.Workflow.Activities.Hosting;
using NUnit.Framework;

namespace Taobao.Workflow.Activities.Test
{
    /// <summary>
    /// 【重要】用于测试调度请求的逻辑
    /// </summary>
    [TestFixture]
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class ResumptionTest : BaseTest
    {
        private static readonly string _chargingBy = "dev_unittest";

        [Test(Description = "创建各类调度请求")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Create()
        {
            var process = this.Prepare();
            var subProcess = this.PrepareSubProcess(process);

            //流程发起调度
            WaitingResumption r = new ProcessStartResumption(process);
            this._resumptionService.Add(r);
            this.Evict(r);
            WaitingResumption r2 = this._resumptionService.Get(r.ID);
            Assert.IsNotNull(r2);
            Assert.IsInstanceOf<ProcessStartResumption>(r2);
            //书签恢复调度
            r = new BookmarkResumption(process, "节点1", "bookmark", "result");
            this._resumptionService.Add(r);
            this.Evict(r);
            r2 = this._resumptionService.Get(r.ID);
            Assert.IsNotNull(r2);
            Assert.IsInstanceOf<BookmarkResumption>(r2);
            //错误恢复调度
            r = new ErrorResumption(process, 0);
            this._resumptionService.Add(r);
            this.Evict(r);
            r2 = this._resumptionService.Get(r.ID);
            Assert.IsNotNull(r2);
            Assert.IsInstanceOf<ErrorResumption>(r2);
            //人工任务创建调度
            var h = new HumanActivityInstance(process.ID, 0, 1, "节点", "bookmark", new string[] { "houkun" });
            this._processService.CreateActivityInstance(h);
            r = new WorkItemCreateResumption(process, h);
            this._resumptionService.Add(r);
            this.Evict(r);
            r2 = this._resumptionService.Get(r.ID);
            Assert.IsNotNull(r2);
            Assert.IsInstanceOf<WorkItemCreateResumption>(r2);
            //流程完成调度
            TestHelper.ChangeProcessStatus(subProcess, ProcessStatus.Completed);
            r = new SubProcessCompleteResumption(process, subProcess);
            this._resumptionService.Add(r);
            this.Evict(r);
            r2 = this._resumptionService.Get(r.ID);
            Assert.IsNotNull(r2);
            Assert.IsInstanceOf<SubProcessCompleteResumption>(r2);
            //子流程启动调度
            var sub = new SubProcessActivityInstance(process.ID, 0, 1, "节点", "bookmark");
            this._processService.CreateActivityInstance(sub);
            r = new SubProcessCreateResumption(process, sub);
            this._resumptionService.Add(r);
            this.Evict(r);
            r2 = this._resumptionService.Get(r.ID);
            Assert.IsNotNull(r2);
            Assert.IsInstanceOf<SubProcessCreateResumption>(r2);
            //节点实例取消调度
            r = new ActivityInstanceCancelResumption(process, h);
            this._resumptionService.Add(r);
            this.Evict(r);
            r2 = this._resumptionService.Get(r.ID);
            Assert.IsNotNull(r2);
            Assert.IsInstanceOf<ActivityInstanceCancelResumption>(r2);
        }

        [Test(Description = "负责调度")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Charge()
        {
            this.Create();
            this.Create();
            var all = this._resumptionService.ChargeResumption(_chargingBy, 20);
            Assert.Greater(all.Count(), 0);
            this.Create();
            this.AssertCharge<ProcessStartResumption>();
            this.AssertCharge<SubProcessCompleteResumption>();
            this.AssertCharge<BookmarkResumption>();
            this.AssertCharge<ErrorResumption>();
            this.AssertCharge<WorkItemCreateResumption>();
            this.AssertCharge<SubProcessCreateResumption>();
            this.AssertCharge<ActivityInstanceCancelResumption>();
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void AnyValid()
        {
            var process = this.Prepare();
            this.ClearResumption();
            var r1 = new BookmarkResumption(process, "节点1", "bookmark", "result", DateTime.Now.AddMinutes(3));
            var r2 = new BookmarkResumption(process, "节点1", "bookmark", "result");
            r2.SetExecuted();
            this._resumptionService.Add(r1);
            this._resumptionService.Add(r2);
            //排除延迟的
            Assert.IsFalse(this._resumptionService.AnyValidAndUnExecutedAndNoErrorResumptionsExceptDelayAt(process, DateTime.Now.AddMinutes(2)));
            //排除指定
            Assert.IsFalse(this._resumptionService.AnyValidAndUnExecutedResumptions(process, r1));
            //延迟的也属于有效
            Assert.IsTrue(this._resumptionService.AnyValidAndUnExecutedResumptions(process, r2));
        }

        //以下调度测试基于默认的流程进行断言

        [Test(Description = "流程开始运行")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void ProcessStartResumption()
        {
            var process = this.Prepare();
            Assert.AreEqual(ProcessStatus.Running, process.Status);

            var r = new ProcessStartResumption(process);
            this._resumptionService.Add(r);

            //执行流程启动调度
            this._scheduler.Resume(r);
            Thread.Sleep(1000);

            this.Evict(process);
            this.AssertExecutedResumption(r);

            process = this._processService.GetProcess(process.ID);
            //完成后new->running
            Assert.AreEqual(ProcessStatus.Running, process.Status);
        }

        [Test(Description = "任务创建")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItemCreateResumption()
        {
            var process = this.Prepare();
            //由于创建时会产生ProcessStart，应先取消不必要的调度请求
            this._resumptionService.CancelAll(process);

            TestHelper.ChangeProcessStatus(process, ProcessStatus.Running);
            var h = new HumanActivityInstance(process.ID, 0, 1, "节点1", "1", new string[] { "houkun" });
            this._processService.CreateActivityInstance(h);
            var r = new WorkItemCreateResumption(process, h);
            this._resumptionService.Add(r);

            //执行人工任务创建调度
            this._scheduler.Resume(r);
            this.AssertExecutedResumption(r);

            this.Evict(process);
            var process2 = this._processService.GetProcess(process.ID);
            //任务数=1
            Assert.AreEqual(1, this._workItemService.GetWorkItems(process2).Count());
            Assert.AreEqual("节点1", this._workItemService.GetWorkItems(process2).First().ActivityName);
            Assert.AreEqual("houkun", this._workItemService.GetWorkItems(process2).First().Actioner.UserName);
        }

        [Test(Description = "书签恢复")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void BookmakrResumption()
        {
            var process = this.Prepare();

            //启动流程
            this.Evict(process);
            var r1 = new ProcessStartResumption(process);
            this._resumptionService.Add(r1);
            this._scheduler.Resume(r1);
            Thread.Sleep(1000);

            //获取任务对应的书签名
            var bookmarkName = this._sessionManager
                .OpenSession()
                .CreateCriteria<WorkItemCreateResumption>()
                .Add(Expression.Eq("Process", process))
                .UniqueResult<WorkItemCreateResumption>().HumanActivityInstance.ReferredBookmarkName;

            _log.Info(bookmarkName);

            //恢复书签
            this.Evict(process);
            var persisted = this._processService.GetProcess(process.ID);
            var r2 = new BookmarkResumption(persisted, "节点", bookmarkName, "Agree");
            this._resumptionService.Add(r2);
            this._scheduler.Resume(r2);
            Thread.Sleep(3000);
            this.AssertExecutedResumption(r2);
        }

        [Test(Description = "错误恢复")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void ErrorResumption()
        {
            var process = this.Prepare();
            //启动流程
            this.Evict(process);
            var r1 = new ProcessStartResumption(process);
            this._resumptionService.Add(r1);
            this._scheduler.Resume(r1);
            Thread.Sleep(1000);
            //错误恢复
            this.Evict(process);
            var persisted = this._processService.GetProcess(process.ID);
            //将流程置为Running
            TestHelper.ChangeProcessStatus(persisted, ProcessStatus.Running);
            var r2 = new ErrorResumption(persisted, 1);
            this._resumptionService.Add(r2);
            this._scheduler.Resume(r2);
            Thread.Sleep(1000);
            this.AssertExecutedResumption(r2);
        }

        [Test(Description = "子流程创建")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void SubProcessCreateResumption()
        {
            //父流程
            var parent = this.PrepareParentProcess();
            //创建子流程类型
            this.CreateProcessType(SubProcessTypeName);
            //创建子流程节点实例
            var sub = new SubProcessActivityInstance(parent.ID, SubProcessNodeIndex, 1, SubProcessNode, "bookmark");
            this._processService.CreateActivityInstance(sub);

            //子流程创建调度
            var r = new SubProcessCreateResumption(parent, sub);
            this._resumptionService.Add(r);
            this._scheduler.Resume(r);
            this.AssertExecutedResumption(r);
            Assert.IsTrue(sub.SubProcessId.HasValue);

            this.EvictAll();
            //发起的子流程
            var subProcess = this._processService.GetProcess(sub.SubProcessId.Value);
            Assert.AreEqual(parent.ID, subProcess.ParentProcessId.Value);
            Assert.AreEqual(SubProcessTypeName, subProcess.ProcessType.Name);
        }

        [Test(Description = "子流程结束")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void SubProcessCompleteResumption()
        {
            #region 父子流程创建
            //父流程
            var parent = this.PrepareParentProcess();
            //将父流程设置到子流程节点
            TestHelper.UpdateCurrentNode(parent, SubProcessNodeIndex);
            //运行父子流程
            var start = new ProcessStartResumption(parent);
            this._resumptionService.Add(start);
            this._scheduler.Resume(start);
            Thread.Sleep(1000);
            //运行子流程创建调度
            var r_sub = this._resumptionService.GetValidWaitingResumptions(parent).First(o =>
                o is SubProcessCreateResumption) as SubProcessCreateResumption;
            this._scheduler.Resume(r_sub);
            parent = this._processService.GetProcess(parent.ID);
            //获取子流程
            var subProcess = this._processService.GetProcess(r_sub.SubProcessActivityInstance.SubProcessId.Value);
            #endregion

            //创建子流程完成调度项
            TestHelper.ChangeProcessStatus(parent, ProcessStatus.Running);
            TestHelper.ChangeProcessStatus(subProcess, ProcessStatus.Completed);
            var r = new SubProcessCompleteResumption(parent, subProcess);
            this._resumptionService.Add(r);

            //执行子流程结束调度
            this._scheduler.Resume(r);
            //同时会恢复父流程书签
            Thread.Sleep(5000);
            this.AssertExecutedResumption(r);

            this.Evict(subProcess);
            this.Evict(parent);
            parent = this._processService.GetProcess(parent.ID);
            subProcess = this._processService.GetProcess(subProcess.ID);
            //子流程应为完成状态
            Assert.AreEqual(ProcessStatus.Completed, subProcess.Status);
            //由于父流程在最后一个节点，也应为完成
            Assert.AreEqual(ProcessStatus.Completed, parent.Status);
        }

        [Test(Description = "节点实例取消")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void ActivityInstanceCancelResumption()
        {
            var process = this.PrepareParentProcess();

            #region 人工节点取消
            //运行流程
            var start = new ProcessStartResumption(process);
            this._resumptionService.Add(start);
            this._scheduler.Resume(start);
            Thread.Sleep(1000);
            //查找当前的人工节点
            var human = this._sessionManager
                .OpenSession()
                .CreateCriteria<HumanActivityInstance>()
                .Add(Expression.Eq("ProcessId", process.ID))
                .UniqueResult<HumanActivityInstance>();
            this.Evict(process);
            process = this._processService.GetProcess(process.ID);
            //取消人工节点
            var r = new ActivityInstanceCancelResumption(process, human);
            this._resumptionService.Add(r);
            this._scheduler.Resume(r);
            this.AssertExecutedResumption(r);
            #endregion

            #region 子流程节点取消
            //在子流程节点处运行流程
            TestHelper.UpdateCurrentNode(process, SubProcessNodeIndex);
            start = new ProcessStartResumption(process);
            this._resumptionService.Add(start);
            this._scheduler.Resume(start);
            Thread.Sleep(1000);
            //运行子流程创建调度
            var r_sub = this._resumptionService.GetValidWaitingResumptions(process).First(o =>
                o is SubProcessCreateResumption) as SubProcessCreateResumption;
            this._scheduler.Resume(r_sub);

            //创建节点取消调度项
            r = new ActivityInstanceCancelResumption(process, r_sub.SubProcessActivityInstance);
            this._resumptionService.Add(r);

            //子流程仍处于running或调度状态无法撤销
            try { this._scheduler.Resume(r); Assert.IsTrue(false); }
            catch (Exception e) { _log.Info(e.Message); }
            //将子流程的调度项清空并置为非Running后
            var subProcess = this._processService.GetProcess(r_sub.SubProcessActivityInstance.SubProcessId.Value);
            this.ClearResumption(subProcess);
            TestHelper.MarkProcessAsActive(subProcess);
            this._scheduler.Resume(r);
            this.AssertExecutedResumption(r);
            //子流程被删除
            Assert.AreEqual(ProcessStatus.Deleted, subProcess.Status);
            #endregion
        }

        [Test(Description = "人工任务超时事件升级")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void EscalationJobResumption()
        {
            //在EscalationRuleTest中完成
        }

        private Process Prepare()
        {
            return this.CreateProcess();
        }
        private Process PrepareParentProcess()
        {
            return this.CreateProcess("houkun", SubProcessTypeName);
        }
        private Process PrepareSubProcess(Process parent)
        {
            var subProcess = new Process("SubProcess From UnitTest at " + DateTime.Now
              , this.CreateProcessType(SubProcessTypeName)
              , this.GetUser("houkun")
              , 0
              , null
              , parent);
            this._processService.Create(subProcess);
            return subProcess;
        }

        private void ClearResumption()
        {
            this._sessionManager.OpenStatelessSession().CreateSQLQuery("delete from NTFE_WaitingResumption").ExecuteUpdate();
        }
        private void AssertCharge<T>()
        {
            var list = this._resumptionService.ChargeResumption<T>(_chargingBy, 10);
            Assert.Greater(list.Count(), 0);
            list.ToList().ForEach(o => Assert.IsInstanceOf<T>(this._resumptionService.Get(o.Item1)));
        }
        private void AssertExecutedResumption(WaitingResumption r)
        {
            Assert.IsTrue(r.IsExecuted);
            Assert.IsFalse(r.IsValid);
        }
    }
}
