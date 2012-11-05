using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Taobao.Infrastructure.Services;
using Taobao.Workflow.Activities.Client;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Taobao.Workflow.Host.Test
{
    /// <summary>
    /// 对NTFE-BPM的服务接口集成测试
    /// </summary>
    [TestFixture]
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class EngineTest : BaseTest
    {
        private string _processType = "EngineClientTest";
        private string _node1 = "节点1";
        private string _node2 = "节点2";
        private string _node3 = "节点3";
        private string _node4 = "节点4";
        private string _node4_1 = "并行子节点1";
        private string _node4_2 = "并行子节点2";
        private string _variable1 = "username1";
        private string _variable2 = "username2";

        //用于测试的流程 变量username1|username2 
        //节点1 主管&发起人审批 同意|否决 会签 1人否决则结束
        //节点2 发起人确认 同意|否决 否决则退回节点1
        //节点3 由username1归档 完成
        //节点4 并行
        //并行子节点1 发起人 完成
        //并行子节点2 主管 完成

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void GetEngine()
        {
            _log.Info(this._clientApi);
            _log.Info(this._managementApi);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Agent()
        {
            var engine = this._clientApi;
            engine.RevokeAllAgents(_originator);

            //不能将自己设置自己的代理
            try { engine.CreateAgent(_originator, _originator, DateTime.Now, DateTime.Now.AddDays(1), null); Assert.IsTrue(false); }
            catch (Exception e) { _log.Info(e.Message); }

            var u = "xiaoxuan" + DateTime.Now;
            engine.CreateAgent(u, _originator, DateTime.Now, DateTime.Now.AddDays(1), null);
            //不能重复设置代理人
            try { engine.CreateAgent(u, _originator, DateTime.Now, DateTime.Now.AddDays(1), null); Assert.IsTrue(false); }
            catch (Exception e) { _log.Info(e.Message); }
            //分流程代理
            engine.CreateAgent("xiaoxuan" + DateTime.Now.AddDays(1), _originator, DateTime.Now, DateTime.Now.AddDays(1), new string[] { _processType });

            engine.GetAgents(_originator).ToList().ForEach(o =>
                _log.InfoFormat("{0}|{1}|{2}|{3}|{4}|{5}|{6}|"
                , o.ActAsUserName
                , o.UserName
                , o.BeginTime
                , o.CreateTime
                , o.EndTime
                , o.Range
                , string.Join("/", o.ProcessTypeNames)));
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void ProcessType()
        {
            var engine = this._managementApi;

            engine.GetProcessTypes().ToList().ForEach(o => _log.InfoFormat(
                "{0}|{1}|{2}|{3}|{4}|{5}"
                , o.Name
                , o.CreateTime
                , o.Description
                , o.Version
                , string.Join("->", o.ActivityNames)
                , string.Join("$", o.DataFields)));

            //流程历史版本
            var histories = engine.GetHistoriesOfProcessType(_processType);

            if (histories.Count() > 0)
            {
                var old = histories.First();
                //设置当前版本
                engine.SetCurrentProcessType(_processType, old.Version);

                //验证
                var current = engine.GetProcessTypes().FirstOrDefault(o => o.Name.Equals(_processType));
                Assert.AreEqual(old.Version, current.Version);
            }

            //发布流程
            this.CreateProcessType();
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void ProcessType_SetCurrent()
        {
            var engine = this._managementApi;
            var t = engine.GetProcessTypes().FirstOrDefault(o => o.Name.Equals("ClientTest"));
            engine.SetCurrentProcessType(t.Name, t.Version);
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Process()
        {
            var engine = this._clientApi;
            var p = this.CreateProcess();
            this.Idle();

            Assert.AreEqual(p.Title, engine.GetProcess(p.ID).Title);
            Assert.AreEqual(_originator, engine.GetProcess(p.ID).DataFields[_variable1]);
            //更新
            engine.UpdateDataFields(p.ID, new Dictionary<string, string>() { { _variable1, _superior }, { _variable2, _originator } });
            Assert.AreEqual(_superior, engine.GetProcess(p.ID).DataFields[_variable1]);
            Assert.AreEqual(_originator, engine.GetProcess(p.ID).DataFields[_variable2]);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void ProcessManagement()
        {
            var engine = this._managementApi;
            //搜索流程
            var info = engine.GetProcessesByKeyword("UnitTest", 1, 20);
            _log.Info(info.Total);
            this.TraceProcess(info.Processes);

            //发起流程
            var p = this.CreateProcess();
            this.Idle();
            //切换实例的流程版本
            //engine.RedirectProcess()
            //跳转
            _log.Info("跳转...");
            engine.RedirectProcess(p.ID, _node2);
            this.Idle();
            //停止
            _log.Info("停止...");
            engine.StopProcess(p.ID);
            Assert.AreEqual(ProcessStatus.Stopped, engine.GetProcess(p.ID).Status);
            //启动
            _log.Info("启动...");
            engine.RestartProcess(p.ID);
            Assert.AreEqual(ProcessStatus.Running, engine.GetProcess(p.ID).Status);
            this.Idle();
            Assert.AreEqual(ProcessStatus.Active, engine.GetProcess(p.ID).Status);
            //删除
            _log.Info("删除...");
            engine.DeleteProcess(p.ID);
            Assert.IsNull(engine.GetProcess(p.ID));
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void ProcessError()
        {
            var engine = this._clientApi;
            var wroingId = Guid.NewGuid();
            Assert.IsNull(engine.GetProcess(wroingId));

            try { engine.UpdateDataFields(wroingId, new Dictionary<string, string>() { { "key1", "2" }}); }
            catch (Exception e) { _log.Info(e.Message); }
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_1_2_3_4()
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
            this.TraceWorkItem(ws.ToArray());
            var w = this.GetAndAssertWorkItem(ws, _node1);
            //get
            engine.GetWorkItem(w.ID, _superior);
            //open
            engine.OpenWorkItem(w.ID, _superior);
            Assert.AreEqual(WorkItemStatus.Open, engine.GetWorkItem(w.ID, _superior).Status);
            //由于已经修改成支持任意action以支持激活隐含规则
            //wrong action
            //try { engine.ExecuteWorkItem(w.ID, _superior, "do", null); }
            //catch (Exception e) { _log.Info(e.Message); }
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

            _log.Info("节点2处理...");
            #region 节点2 同意 设置username1=_userName
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node2);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _originator, "同意", new Dictionary<string, string>() { { _variable1, _originator } });
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            _log.Info("节点3处理...");
            #region 节点3 完成
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node3);
            //execute 完成
            engine.ExecuteWorkItem(w.ID, _originator, "完成", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            _log.Info("节点4处理...");
            #region 并行子节点1 发起人
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node4_1);
            //execute 完成
            engine.ExecuteWorkItem(w.ID, _originator, "完成", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));
            this.Idle();
            #endregion
            #region 并行子节点2 主管
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_superior, p.ID, null), _node4_2);
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
        public void WorkItem_1()
        {
            var engine = this._clientApi;
            //发起流程
            var p = this.CreateProcess();
            this.Idle();

            _log.Info("节点1处理...");
            #region 节点1 否决
            //主管
            var w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_superior, p.ID, null), _node1);
            //execute 否决
            engine.ExecuteWorkItem(w.ID, _superior, "否决", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _superior));

            //发起人
            var ws = engine.GetWorkItemsByProcess(_originator, p.ID, null);
            //任务应被收回
            Assert.AreEqual(0, ws.Length);
            Thread.Sleep(5000);
            #endregion

            //流程结束
            Assert.AreEqual(ProcessStatus.Completed, engine.GetProcess(p.ID).Status);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_1_()
        {
            var engine = this._clientApi;
            //发起流程
            var p = this.CreateProcess();
            this.Idle();

            _log.Info("节点1处理...");
            #region 节点1 否决
            //主管
            var w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_superior, p.ID, null), _node1);
            //execute 否决
            engine.ExecuteWorkItem(w.ID, _superior, "同意", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _superior));

            //发起人
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node1);
            //execute 否决
            engine.ExecuteWorkItem(w.ID, _originator, "否决", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));
            Thread.Sleep(5000);
            #endregion

            //流程结束
            Assert.AreEqual(ProcessStatus.Completed, engine.GetProcess(p.ID).Status);
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItem_1_2_1()
        {
            var engine = this._clientApi;

            //发起流程
            var p = this.CreateProcess();
            this.Idle();

            _log.Info("节点1处理...");
            #region 节点1 同意
            //主管
            var w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_superior, p.ID, null), _node1);
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

            _log.Info("节点2处理...");
            #region 节点2 否决
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node2);
            //execute 同意
            engine.ExecuteWorkItem(w.ID, _originator, "否决", null);
            Assert.IsNull(engine.GetWorkItem(w.ID, _originator));

            this.Idle();
            #endregion

            _log.Info("节点1处理...");
            #region 节点1
            //主管
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_superior, p.ID, null), _node1);
            //发起人
            w = this.GetAndAssertWorkItem(engine.GetWorkItemsByProcess(_originator, p.ID, null), _node1);
            #endregion
        }

        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItemManagement()
        {
            var engine = this._managementApi;
            TraceWorkItem(engine.GetWorkItems(this._originator));
        }
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void WorkItemError()
        {
            var engine = this._clientApi;
            var wrongId = 1000000000;

            Assert.IsNull(engine.GetWorkItem(wrongId, _originator));
            Assert.AreEqual(0, engine.GetWorkItems(DateTime.Now.ToString()).Count());

            try { engine.OpenWorkItem(wrongId, _originator); }
            catch (Exception e) { _log.Info(e.Message); }

            try { engine.ExecuteWorkItem(wrongId, _originator, null, null); }
            catch (Exception e) { _log.Info(e.Message); }
        }

        private void CreateProcessType()
        {
            this.CreateProcessType(this._processType, Resource.Workflow, Resource.Settings);
        }
        private Process CreateProcess()
        {
            this.CreateProcessType();
            return this.CreateProcess(_processType, new Dictionary<string, string>() { { _variable1, this._originator } });
        }
        private WorkItem GetAndAssertWorkItem(WorkItem[] ws, string activityName)
        {
            this.TraceWorkItem(ws);
            Assert.AreEqual(1, ws.Length);
            var w = ws.First();
            Assert.IsNotNull(w);
            Assert.AreEqual(activityName, w.ActivityName);
            return w;
        }
        private void TraceProcess(params Process[] processes)
        {
            processes.ToList().ForEach(o => _log.InfoFormat(
                "{0}|{1}|{2}|{3}|{4}|{5}"
                , o.ID
                , o.Title
                , o.Status
                , o.ProcessType.Name
                , o.Originator
                , string.Join("$", o.DataFields.Select(p => p.Key + "=" + p.Value)).ToArray()));
        }
        private void TraceWorkItem(params WorkItem[] workItems)
        {
            workItems.ToList().ForEach(o => _log.InfoFormat("{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}"
                , o.ID
                , o.Actioner
                , string.Join("$", o.Actions)
                , o.ActivityName
                , o.ArrivedTime
                , o.CreateTime
                , o.OriginalActioner
                , o.ProcessId
                , o.Status
                , o.Url));
        }
    }
}