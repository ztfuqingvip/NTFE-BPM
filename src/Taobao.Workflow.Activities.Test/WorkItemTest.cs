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
using System.Diagnostics;
using NUnit.Framework;

namespace Taobao.Workflow.Activities.Test
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture]
    public class WorkItemTest : BaseTest
    {
        private static Random _rd = new Random();
        private static string USER;
        private static readonly string TYPENAME = "WorkItemTest";

        [SetUp]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize]
        public void Prepare()
        {
            USER = "houkun" + DateTime.Now + _rd.Next();
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Get()
        {
            var p = this.CreateProcess(USER, TYPENAME);
            var type = this._processTypeService.GetProcessType(TYPENAME);
            var user = this._userService.GetUserWhatever(USER);
            var instance1 = new HumanActivityInstance(p.ID, 1, 1, "审批1", "1", new string[] { "houkun" });
            var instance2 = new HumanActivityInstance(p.ID, 1, 1, "审批2", "1", new string[] { "houkun" });

            this._processService.CreateActivityInstance(instance1);
            this._processService.CreateActivityInstance(instance2);

            this._workItemService.Create(new WorkItem(user, p, instance1));
            this._workItemService.Create(new WorkItem(user, p, instance1));
            this._workItemService.Create(new WorkItem(user, p, instance2));

            Assert.AreEqual(3, this._workItemService.GetWorkItems(user).Count());
            Assert.AreEqual(3, this._workItemService.GetWorkItems(user, p, null).Count());
            Assert.AreEqual(2, this._workItemService.GetWorkItems(user, p, "审批1").Count());

            //facade

            Assert.AreEqual(3, this._managementApi.GetWorkItems(USER).Length);
            Assert.AreEqual(3, this._managementApi.GetWorkItemsByProcess(USER, p.ID, null).Length);
            Assert.AreEqual(2, this._managementApi.GetWorkItemsByProcess(USER, p.ID, "审批1").Length);
            _log.Info(this._managementApi.GetAllWorkItemsByType(TYPENAME).Length);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Search()
        {
            Assert.AreEqual(0, this._managementApi.SearchWorkItems(new Management.WorkItemQuery(), 1, 20).Total);

            _log.Info("搜索：" + this._managementApi.SearchWorkItems(new Management.WorkItemQuery()
            {
                ProcessTypeName = TYPENAME,
                Status = new Client.WorkItemStatus[] { },
                Actioner = "A2283BCD-6AB1-401E-ABB1-9F8300EE29D6",
                ProcessTitle = "1",
                ActivityName = "节点1",
                CreateTo = DateTime.Now,
                CreateFrom = DateTime.Now
            }, 1, 20).Total);
        }
        [Test(Description = "完整执行流程中每个人工节点的任务")]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Execute()
        {
            Taobao.Activities.Hosting.WorkflowInstance.IsEnableDebug = false;

            //以下描述顺序流程过程
            Guid id = this.CreateProcess(USER).ID;

            this._scheduler.Run();
            Thread.Sleep(5000);
            var user = this._userService.GetUserWhatever(USER);
            var superior = this._userService.GetUserWhatever(Taobao.Workflow.Activities.Test.Stub.UserHelper.Superior);
            Process p = null;

            var username1 = this._userService.GetUserWhatever("houkun");
            #region 节点1
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                Assert.AreEqual(ProcessStatus.Active, p.Status);
                var w1 = this._workItemService.GetWorkItems(user, p, null).First();
                Assert.AreEqual("节点1", w1.ActivityName);
                Assert.AreEqual(WorkItemStatus.New, w1.Status);
                this._workItemService.Execute(w1.ID, user, "同意", new Dictionary<string, string>() { { VariableUser, username1.UserName } });
                r.Flush();
            }
            Thread.Sleep(20000);
            #endregion

            #region 节点2 delay 10s
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                Assert.AreEqual(ProcessStatus.Active, p.Status);
                var w2 = this._workItemService.GetWorkItems(user, p, null).First();
                Assert.AreEqual("节点2", w2.ActivityName);
                Assert.AreEqual(WorkItemStatus.New, w2.Status);
                this._workItemService.Execute(w2.ID, user, "完成", null);
                r.Flush();
            }
            Thread.Sleep(5000);
            #endregion

            #region 节点3 OneAtOnce
            //originator
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                //oneAtOnce
                Assert.AreEqual(1, this._workItemService.GetWorkItems(p).Count());
                Assert.AreEqual(ProcessStatus.Active, p.Status);
                var w3 = this._workItemService.GetWorkItems(user, p, null).First();
                Assert.AreEqual("节点3", w3.ActivityName);
                Assert.AreEqual(WorkItemStatus.New, w3.Status);
                this._workItemService.Execute(w3.ID, user, "完成", null);
                r.Flush();
            }
            Thread.Sleep(5000);
            //superior
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                Assert.AreEqual(ProcessStatus.Active, p.Status);
                var w3 = this._workItemService.GetWorkItems(superior, p, null).First();
                Assert.AreEqual("节点3", w3.ActivityName);
                Assert.AreEqual(WorkItemStatus.New, w3.Status);
                this._workItemService.Execute(w3.ID, superior, "完成", null);
                r.Flush();
            }
            //username1 slot占满被取消
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                Assert.AreEqual(0, this._workItemService.GetWorkItems(username1, p, null).Count());
            }
            #endregion

            //节点4 Server
            Thread.Sleep(7000);

            #region 并行节点 WakeUpDelayedActivityInstance
            //并行子节点1 originator 无限延迟 主动唤醒
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                this._processService.WakeUpDelayedActivityInstance(p, "并行子节点1");
                r.Flush();
            }
            Thread.Sleep(10000);
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                Assert.AreEqual(ProcessStatus.Active, p.Status);
                var w = this._workItemService.GetWorkItems(user, p, null).First();
                Assert.AreEqual("并行子节点1", w.ActivityName);
                Assert.AreEqual(WorkItemStatus.New, w.Status);
                this._workItemService.Execute(w.ID, user, "完成", null);
                r.Flush();
            }
            Thread.Sleep(5000);
            //并行子节点2 superior 被取消
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                Assert.AreEqual(ProcessStatus.Active, p.Status);
                Assert.AreEqual(0, this._workItemService.GetWorkItems(superior, p, null).Count());
            }
            Thread.Sleep(5000);
            #endregion

            #region 节点6 allatonce slot=2 actioner=1
            //originator
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                Assert.AreEqual(ProcessStatus.Active, p.Status);
                var w3 = this._workItemService.GetWorkItems(user, p, null).First();
                Assert.AreEqual("节点6", w3.ActivityName);
                Assert.AreEqual(WorkItemStatus.New, w3.Status);
                this._workItemService.Execute(w3.ID, user, "同意", null);
                r.Flush();
            }
            Thread.Sleep(5000);
            #endregion

            #region 节点7 allatonce slot=1 actioner=2
            //originator
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                Assert.AreEqual(ProcessStatus.Active, p.Status);
                var w3 = this._workItemService.GetWorkItems(user, p, null).First();
                Assert.AreEqual("节点7", w3.ActivityName);
                Assert.AreEqual(WorkItemStatus.New, w3.Status);
                this._workItemService.Execute(w3.ID, user, "同意", null);
                r.Flush();
            }
            //superior
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                Assert.AreEqual(0, this._workItemService.GetWorkItems(superior, p, null).Count());
            }
            Thread.Sleep(5000);
            #endregion

            //完成
            using (var r = CodeSharp.Core.Castles.NHibernateRepositoryUtility.UnmanagedSession())
            {
                p = this._processService.GetProcess(id);
                Assert.AreEqual(ProcessStatus.Completed, p.Status);
            }
            this._scheduler.Stop();
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Redirect()
        {
            var p = this.CreateProcess(USER);
            this._scheduler.Run();
            Thread.Sleep(5000);
            this._scheduler.Stop();

            var user = this._userService.GetUser(USER);
            var w = this._workItemService.GetWorkItems(user, p, null).First();
            this._workItemService.Redirect(w.ID, user, this._userService.GetUserWhatever("xiaoxuan"));
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Execute_Concurrent()
        {
            var slotCount = 8;
            this.Execute(slotCount, "atMostOf(" + slotCount / 2 + ",'同意')", true);
            this.Execute(slotCount, "atMostOf(" + slotCount / 2 + ",'同意')", false);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Execute_Concurrent2()
        {
            var slotCount = 8;

            var d = DateTime.Now;
            while (DateTime.Now - d < TimeSpan.FromMilliseconds(10000))
                this.Execute(slotCount, "atMostOf(" + slotCount / 2 + ",'同意')", true);

            d = DateTime.Now;
            while (DateTime.Now - d < TimeSpan.FromMilliseconds(10000))
                this.Execute(slotCount, "atMostOf(" + slotCount / 2 + ",'同意')", false);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Execute_Slot_AllAtOnce()
        {

        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Open_SlotLessThanActioner()
        {
            var type = new ProcessType("SlotTest"
                , new ProcessType.WorkflowDefinition("")
                , new HumanSetting(0
                    , "节点1"
                    , new string[] { "同意" }
                    , 2//slot=2
                    , HumanSetting.SlotDistributionMode.AllAtOnce
                    , ""
                    , null
                    , new HumanActionerRule("originator")
                    , null
                    , null
                    , false)) { Group = "test" };
            this._processTypeService.Create(type);
            var user = this._userService.GetUserWhatever(USER);

            var p = new Process("slot test", type, user);
            this._processService.Create(p);
            var instance = new HumanActivityInstance(p.ID, 0, 1, "节点1", "bookmark", new string[] { user.UserName });

            this._processService.CreateActivityInstance(instance);

            var w1 = new WorkItem(user, p, instance);
            var w2 = new WorkItem(user, p, instance);
            var w3 = new WorkItem(user, p, instance);
            this._workItemService.Create(w1);
            this._workItemService.Create(w2);
            this._workItemService.Create(w3);

            Assert.AreEqual(WorkItemStatus.Open, this._workItemService.Open(w1.ID, user).Status);
            Assert.AreEqual(WorkItemStatus.Open, this._workItemService.Open(w2.ID, user).Status);

            try
            {
                this._workItemService.Open(w3.ID, user);
                Assert.IsTrue(false);
            }
            catch (Exception e)
            {
                _log.Info(e.Message);
            }

            Assert.IsNull(this._workItemService.GetWorkItem(w3.ID));
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Release()
        {
            var type = new ProcessType("ReleaseSlotTest"
                  , new ProcessType.WorkflowDefinition("")
                  , new HumanSetting(0
                      , "节点1"
                      , new string[] { "同意" }
                      , 2//slot=2
                      , HumanSetting.SlotDistributionMode.AllAtOnce
                      , ""
                      , null
                      , new HumanActionerRule("originator")
                      , null
                      , null
                      , false)) { Group = "test" };
            this._processTypeService.Create(type);
            var user = this._userService.GetUserWhatever(USER);

            var p = new Process("release slot test", type, user);
            this._processService.Create(p);
            var instance = new HumanActivityInstance(p.ID, 0, 1, "节点1", "bookmark", new string[] { user.UserName });

            this._processService.CreateActivityInstance(instance);

            //置为open
            var w1 = new WorkItem(user, p, instance);
            TestHelper.ChangeWorkItemStatus(w1, WorkItemStatus.Open);
            var w2 = new WorkItem(user, p, instance);
            TestHelper.ChangeWorkItemStatus(w2, WorkItemStatus.Open);
            //置为没有slot
            var w3 = new WorkItem(user, p, instance);
            TestHelper.ChangeWorkItemStatus(w3, WorkItemStatus.NoSlot);

            this._workItemService.Create(w1);
            this._workItemService.Create(w2);
            this._workItemService.Create(w3);

            //释放
            this._workItemService.Release(w1);
            //释放后w3应为New状态
            Assert.AreEqual(WorkItemStatus.New, this._workItemService.GetWorkItem(w3.ID).Status);
            //w1为New
            Assert.AreEqual(WorkItemStatus.New, this._workItemService.GetWorkItem(w1.ID).Status);
            //w2没有影响
            Assert.AreEqual(WorkItemStatus.Open, this._workItemService.GetWorkItem(w2.ID).Status);
        }

        private void Execute(int slotCount, string rule, bool open)
        {
            #region 准备数据
            var type = new ProcessType("ConcurrentTest"
                , new ProcessType.WorkflowDefinition("")
                , new HumanSetting(0
                    , "节点1"
                    , new string[] { "同意" }
                    , slotCount
                    , HumanSetting.SlotDistributionMode.AllAtOnce
                    , ""
                    , null
                    , new HumanActionerRule("originator")
                //,null
                    , new FinishRule(new Dictionary<string, string>() { { "ok", rule } })
                    , null
                    , false)) { Group = "test" };
            this._processTypeService.Create(type);
            var user = this._userService.GetUserWhatever(USER);

            var p = new Process("ConcurrentTest", type, user);
            this._processService.Create(p);
            var instance1 = new HumanActivityInstance(p.ID, 0, 1, "节点1", "bookmark", new string[] { user.UserName });
            var instance2 = new HumanActivityInstance(p.ID, 0, 2, "节点1", "bookmark", new string[] { user.UserName });
            this._processService.CreateActivityInstance(instance1);
            this._processService.CreateActivityInstance(instance2);
            #endregion

            #region 创建任务
            var list = new List<WorkItem>();
            while (list.Count < 20)
            {
                var w1 = new WorkItem(user, p, instance1);
                this._workItemService.Create(w1);
                list.Add(w1);

                //var w2 = new WorkItem(user, p, instance2);
                //this._workItemService.Create(w2);
                //list.Add(w2);
            }
            #endregion

            var count = 0;

            var w = new Stopwatch();
            w.Start();

            if (open)
            {
                #region open
                list.AsParallel().ForAll(o =>
                {
                    try
                    {
                        this._workItemService.Open(o.ID, user);
                        Interlocked.Increment(ref count);
                    }
                    catch (Exception e)
                    {
                        _log.Info(e.Message);
                    }
                });
                #endregion
            }
            else
            {
                #region execute
                list.AsParallel().ForAll(o =>
                {
                    try
                    {
                        this._workItemService.Execute(o.ID, user, "同意", null);
                        Interlocked.Increment(ref count);
                    }
                    catch (Exception e)
                    {
                        _log.Info(e.Message);
                    }
                });
                #endregion
            }

            w.Stop();

            if (open && slotCount != 0)
                Assert.AreEqual(slotCount, count);
            else if (slotCount != 0)
                Assert.AreEqual(slotCount / 2, count);

            Trace.WriteLine(string.Format("执行[{0}]数量={1}|耗时={2}|平均={3}次/s|平均={4}ms/次"
                , open ? "open" : "execute"
                , list.Count
                , w.ElapsedMilliseconds
                , ((double)list.Count / (double)w.ElapsedMilliseconds) * 1000
                , (double)w.ElapsedMilliseconds / (double)list.Count));
        }
    }
}