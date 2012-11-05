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
using CodeSharp.Core.Services;
using Taobao.Workflow.Activities.Hosting;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 用于提供对内部类/方法/属性等的测试帮助
    /// <remarks>为配合测试内部或私有方法，需要对代码方法结构做相应调整</remarks>
    /// </summary>
    public static class TestHelper
    {
        //WorkItem
        public static void ChangeWorkItemStatus(WorkItem w, WorkItemStatus status)
        {
            w.ChangeStatus(status);
        }
        public static void ChangeWorkItemResult(WorkItem w, string result)
        {
            w.MarkAsExecuted(result);
        }

        //Process
        public static int GetCurrentNode(Process p)
        {
            return p.GetCurrentNode();
        }
        public static string GetCurrentActivityName(Process p)
        {
            return p.ProcessType.GetActivitySetting(p.GetCurrentNode()).ActivityName;
        }
        public static void UpdateCurrentNode(Process p, int i)
        {
            p.UpdateCurrentNode(i);
        }
        public static void ChangeProcessStatus(Process p, ProcessStatus status)
        {
            p.ChangeStatus(status);
        }
        public static void MarkProcessAsActive(Process p)
        {
            p.MarkAsActive();
        }
        public static void MarkProcessAsError(Process p)
        {
            p.MarkAsError(new Exception());
        }
        public static void SetProcessId(Process p, Guid id)
        {
            p.SetId(id);
        }

        //scheduler
        public static IEnumerable<IOrderedEnumerable<long>> PrepareCharge(IEnumerable<Tuple<long, Guid>> list)
        {
            return Scheduler.PrepareCharge(list);
        }
        public static void Resume(Scheduler scheduler, IOrderedEnumerable<long> list)
        {
            scheduler.Resume(list);
        }
        public static void ChargeAndResume(Scheduler scheduler)
        {
            scheduler.MarkAsRunning();
            scheduler.ChargeAndResume();
        }
    }
}
