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
    /// 用于记录流程运行中产生的人工节点信息
    /// <remarks>同时用于描述人工节点实例</remarks>
    /// </summary>
    public class HumanActivityInstance : ActivityInstanceBase
    {
        /// <summary>
        /// 获取关联的书签名
        /// </summary>
        public virtual string ReferredBookmarkName { get; private set; }

        private string _actioners { get; set; }
        /// <summary>
        /// 获取执行人用户名列表 UserName
        /// </summary>
        public virtual string[] Actioners
        {
            get { return this._actioners.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries); }
        }
        //已创建任务的执行人用户名
        private string _alreadyActioners { get; set; }
        private string[] AlreadyActioners
        {
            get { return (this._alreadyActioners ?? "").Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries); }
        }

        protected HumanActivityInstance()
            : base()
        {
            this._alreadyActioners = string.Empty;
        }
        public HumanActivityInstance(Guid processId
            , int flowNodeIndex
            , long workflowActivityInstanceId
            , string activityName
            , string bookmarkName
            , string[] actioners)
            : base(processId
            , flowNodeIndex
            , workflowActivityInstanceId
            , activityName)
        {
            this.ReferredBookmarkName = bookmarkName;

            if (actioners == null || actioners.Length == 0)
                throw new InvalidOperationException("至少需要一个执行人");
            this._actioners = string.Join(";", actioners);

            this.Validate();
        }

        //获取下一个任务执行人
        protected internal virtual string GetNextUser(int slot, bool isUsingSlot)
        {
            var actioner = this.Actioners;
            var already = this.AlreadyActioners;
            return isUsingSlot && slot == already.Length
                ? null
                : actioner.FirstOrDefault(o => !already.Contains(o));
        }
        protected internal virtual IEnumerable<string> GetNextUsers(int slot, bool isUsingSlot)
        {
            var actioner = this.Actioners;
            var already = this.AlreadyActioners;
            return actioner.Where(o => !already.Contains(o)).Take(isUsingSlot
                ? slot - already.Length//HACK:若使用slot则返回可用slot数量下的Next用户
                : actioner.Length);
        }
        protected internal virtual IEnumerable<string> GetUsersWhoNotReady()
        {
            var actioner = this.Actioners;
            var already = this.AlreadyActioners;
            return actioner.Where(o => !already.Contains(o));
        }
        protected internal virtual bool IsReady(User actioner)
        {
            return this._alreadyActioners.IndexOf(actioner.UserName) >= 0;
        }
        protected internal virtual WorkItem CreateNextWorkItem(Process process, IUserService u)
        {
            if (process.ID != this.ProcessId)
                throw new InvalidOperationException("流程实例标识不一致");

            var setting = process.ProcessType.GetHumanSetting(this.ActivityName);
            var next = this.GetNextUser(setting.SlotCount, setting.IsUsingSlot);

            if (string.IsNullOrWhiteSpace(next))
                return null;

            //更新已创建列表
            var temp = this.AlreadyActioners.ToList();
            temp.Add(next);
            this._alreadyActioners = string.Join(";", temp);

            return new WorkItem(u.GetUserWhatever(next), process, this);
        }
        protected internal virtual IEnumerable<WorkItem> CreateAllWorkItem(Process process, IUserService u)
        {
            if (process.ID != this.ProcessId)
                throw new InvalidOperationException("流程实例标识不一致");

            this._alreadyActioners = this._actioners;
            foreach (var a in this.Actioners)
                yield return new WorkItem(u.GetUserWhatever(a), process, this);
        }
        private void Validate()
        {
            if (string.IsNullOrEmpty(this.ReferredBookmarkName))
                throw new InvalidOperationException("ReferredBookmarkName不能为空");
        }
    }
}