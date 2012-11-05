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

using System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Castle.MicroKernel.Registration;
using CodeSharp.Core;
using CodeSharp.Core.Castles;
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities.Application;
using Taobao.Workflow.Activities.Hosting;
using System;
using System.Threading;
using NUnit.Framework;

namespace Taobao.Workflow.Activities.Test
{
    [TestFixture]
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    public class PerfTest : BaseTest
    {
        private User _originator;
        private static readonly int _total = 50;

        [SetUp]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize]
        public void Prepare()
        {
            //发起人
            this._originator = DependencyResolver.Resolve<IUserService>().GetUserWhatever("houkun");
        }

        //部分集成情况下的方法耗时/性能 均是顺序执行
        //服务整体性能由外部集成测试

        [Test(Description = "综合测试")]
        [Ignore]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Full()
        {
            //for (var i = 0; i < 10; i++)
            //{
            this.NewProc();
            this.Resume();
            this.Resume();
            this.Execute();
            //}
        }

        [Test(Description = "人工活动调度测试")]
        [Ignore]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void NewProc()
        {
            var type = this.CreateProcessType("PerfTest");

            var w = new Stopwatch();
            w.Start();

            var total = _total;
            for (var i = 0; i < total; i++)
                this._processService.Create(new Process("perf test at " + i, type, this._originator));

            w.Stop();
            this._log.Info(string.Format("NewProc 总计={0}|耗时约={1}|平均={2}ms"
                , total
                , w.ElapsedMilliseconds
                , w.ElapsedMilliseconds / total));
        }
        [Test(Description = "人工活动调度测试")]
        [Ignore]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Execute()
        {
            var todolist = this._workItemService.GetWorkItems(this._originator).ToList();
            var w = new Stopwatch();
            w.Start();

            foreach (var i in todolist)
                //todolist.AsParallel().ForAll(i =>{
                try
                {
                    this._workItemService.Execute(i.ID
                        , this._originator
                        , i.GetReferredSetting().Actions[0]
                        , new Dictionary<string, string>() { { VariableUser, this._originator.UserName } });
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message);
                }
            //});


            w.Stop();
            this._log.Info(string.Format("Execute 总计={0}|耗时约={1}|平均={2}ms"
                , todolist.Count
                , w.ElapsedMilliseconds
                , (double)w.ElapsedMilliseconds / (double)todolist.Count));
        }
        [Test(Description = "人工活动调度测试")]
        [Ignore]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Resume()
        {
            var list = DependencyResolver.Resolve<IResumptionRepository>()
                .FindAll()
                .Where(o => o.IsValid && !o.IsExecuted);

            var total = list.Count();
            this._log.Info("total=" + total);

            var w = new Stopwatch();
            w.Start();

            foreach (var r in list) this._scheduler.Resume(r);

            w.Stop();
            if (total > 0)
                this._log.Info(string.Format("Resume 总计调度项={0}|耗时约={1}|平均={2}ms"
                    , total
                    , w.ElapsedMilliseconds
                    , (double)w.ElapsedMilliseconds / (double)total));
        }
        [Test(Description = "人工活动调度测试")]
        [Ignore]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void Schedule()
        {
            var sessionManager = DependencyResolver.Resolve<Castle.Facilities.NHibernateIntegration.ISessionManager>();
            var scheduler = DependencyResolver.Resolve<IScheduler>();
            var resumption = DependencyResolver.Resolve<IResumptionRepository>();

            var sql = "select count(*) from NTFE_WaitingResumption where IsValid=:IsValid";
            double total;
            using (var s = sessionManager.OpenSession())
                total = s.CreateSQLQuery(sql).SetBoolean("IsValid", true).UniqueResult<int>();

            this._log.Info("total=" + total);

            var w = new Stopwatch();
            w.Start();
            scheduler.Run();

            var flag = true;
            while (flag)
            {
                using (var s = sessionManager.OpenSession())
                    flag = s.CreateSQLQuery(sql).SetBoolean("IsValid", true).UniqueResult<int>() > 0;
                Trace.WriteLine("Still Running=" + flag);
                if (flag)
                    Thread.Sleep(5000);
            }
            w.Stop();
            scheduler.Stop();
            this._log.Info(string.Format("Schedule 总计调度项={0}|耗时约={1}|平均每秒调度={2}"
                , total
                , w.ElapsedMilliseconds
                , (total / (double)w.ElapsedMilliseconds) * 1000));
        }
    }
}