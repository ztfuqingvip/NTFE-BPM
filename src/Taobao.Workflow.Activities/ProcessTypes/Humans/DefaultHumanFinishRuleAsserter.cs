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

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 完成规则断言
    /// </summary>
    public class DefaultHumanFinishRuleAsserter
    {
        private Execution _workItem;
        private List<Execution> _allOtherWorkItems;
        private string _userAction;
        private int? _slot;

        public DefaultHumanFinishRuleAsserter(int? slot, Execution e, List<Execution> others)
        {
            this._slot = slot;
            this._userAction = e.Result;
            this._workItem = e;
            this._allOtherWorkItems = others;
        }

        //slot是否有效 
        //HACK:执行人大于slot数才生效
        private bool IsSlotValid { get { return this._slot.HasValue && this._allOtherWorkItems.Count + 1 > this._slot; } }
        //所有eq: all('同意')
        public bool All(string action)
        {
            if (!this.AreAllOtherExecuted())
                return false;

            var actionCount = this.AllOtherExecutedCount(action) + (this.IsEquals(this._userAction, action) ? 1 : 0);

            return !this.IsSlotValid
                //没有不等于action的
                ? actionCount == this._allOtherWorkItems.Count + 1
                //执行数大等于slotcount
                : actionCount >= this._slot;
        }
        //至少slot个 eq: atLeaseOf(1,'同意')
        public bool AtLeaseOf(int slot, string action)
        {
            if (!this.AreAllOtherExecuted())
                return false;
            return this.AllOtherExecutedCount(action) + (this.IsEquals(this._userAction, action) ? 1 : 0) >= slot;
        }
        //最多slot个 eq: atMostOf(1,'同意')
        public bool AtMostOf(int slot, string action)
        {
            return this.AllOtherExecutedCount(action) + (this.IsEquals(this._userAction, action) ? 1 : 0) >= slot;
        }
        //没有一个同意 eq: noneOf('同意')
        public bool NoneOf(string action)
        {
            if (!this.AreAllOtherExecuted())
                return false;
            return this.AllOtherExecutedCount(action) + (this.IsEquals(this._userAction, action) ? 1 : 0) == 0;
        }
        //是否所有其他用户已经执行过
        private bool AreAllOtherExecuted()
        {
            //是否所有任务都已经执行
            //HACK:任务执行规则需要考虑cancel和noslot的情况
            var allExecuted = this._allOtherWorkItems.Count(o => o.Status != WorkItemStatus.Executed
                && o.Status != WorkItemStatus.Canceled
                && o.Status != WorkItemStatus.NoSlot) == 0;

            if (this.IsSlotValid)
                return allExecuted;
            else
                //所有都执行或已执行的数量大于或等于
                return allExecuted
                    || this._allOtherWorkItems.Count(o => o.Status == WorkItemStatus.Executed) + 1 >= this._slot;
        }
        //所有执行过的数量
        private int AllOtherExecutedCount(string action)
        {
            return this._allOtherWorkItems.Count(o =>
               o.Status == WorkItemStatus.Executed && o.Result.Equals(action, StringComparison.InvariantCultureIgnoreCase));
        }
        private bool IsEquals(string l, string r)
        {
            return l.Equals(r, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// 执行内容
        /// </summary>
        public class Execution
        {
            public WorkItemStatus Status { get; set; }
            public string Result { get; set; }

            public Execution(string result, WorkItemStatus status)
            {
                this.Status = status;
                this.Result = result;
            }
        }
    }
}