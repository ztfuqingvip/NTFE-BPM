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
using System.Text;
using Castle.MicroKernel.Registration;
using Taobao.Workflow.Activities.Hosting;
using CodeSharp.Core.Services;
using CodeSharp.Core.Castles;
using System.Threading;
using System.Reflection;
using CodeSharp.Core;
using NUnit.Framework;
using Taobao.Activities.Statements;
using Taobao.Workflow.Activities.Statements;
using Taobao.Activities;
using Taobao.Activities.Hosting;

namespace Taobao.Workflow.Activities.Test
{
    [Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]
    [TestFixture(Description = "Hosting.Scheduler主体调度逻辑的测试")]
    public class SchedulerResumptionTest : BaseTest
    {
        [Test]
        [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]
        public void AsParallelResumeDeadlock()
        {
            //不同流程实例并行调度潜在死锁问题验证
            for (var i = 0; i < 5; i++)
            {
                var p1 = this.CreateProcess();
                var p2 = this.CreateProcess();
                var p3 = this.CreateProcess();

                this.SyncScheduleUntil(p1);
                this.SyncScheduleUntil(p2);
                this.SyncScheduleUntil(p3);

                Assert.IsEmpty(this._resumptionService.GetErrorRecords(p1));
                Assert.IsEmpty(this._resumptionService.GetErrorRecords(p2));
                Assert.IsEmpty(this._resumptionService.GetErrorRecords(p3));
            }
        }
    }
}