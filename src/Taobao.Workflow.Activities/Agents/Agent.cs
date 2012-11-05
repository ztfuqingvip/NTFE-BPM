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
using CodeSharp.Core.DomainBase;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 代理信息
    /// </summary>
    public class Agent : EntityBase<Guid>, IAggregateRoot
    {
        /// <summary>
        /// 获取代理人
        /// </summary>
        public virtual User User { get; private set; }
        /// <summary>
        /// 获取被代理人/被扮演者
        /// </summary>
        public virtual User ActAs { get; private set; }
        /// <summary>
        /// 获取创建时间
        /// </summary>
        public virtual DateTime CreateTime { get; private set; }
        /// <summary>
        /// 获取代理生效时间
        /// </summary>
        public virtual DateTime BeginTime { get; private set; }
        /// <summary>
        /// 获取代理失效时间
        /// </summary>
        public virtual DateTime EndTime { get; private set; }
        /// <summary>
        /// 获取代理范围
        /// </summary>
        public virtual ActingRange Range { get; private set; }

        private IList<ProcessActingItem> _actingItems { get; set; }
        /// <summary>
        /// 获取代理的项目
        /// </summary>
        public virtual IEnumerable<ProcessActingItem> ActingItems { get { return this._actingItems; } }

        /// <summary>
        /// 获取或设置是否启用该代理人设置
        /// <remarks>False则为删除，将被认为是历史记录</remarks>
        /// </summary>
        protected internal virtual bool _enable { get; set; }

        /// <summary>
        /// 获取该代理人信息当前是否生效
        /// <remarks></remarks>
        /// </summary>
        public virtual bool IsValid
        {
            get
            {
                return this.BeginTime <= this.EndTime
                    && this.EndTime.Date >= DateTime.Now.Date
                    && this.BeginTime.Date <= DateTime.Now.Date
                    && this._enable;
            }
        }

        protected Agent()
        {
            this.CreateTime = DateTime.Now;
            this._actingItems = new List<ProcessActingItem>();
            this._enable = true;
        }

        public Agent(User user, User actAs, DateTime begin, DateTime end, ActingRange range, params ProcessType[] processTypes)
            : this()
        {
            this.User = user;
            this.ActAs = actAs;
            this.Range = range;

            this.ChangeTime(begin, end);

            if (range != ActingRange.All
                && (processTypes == null || processTypes.Length == 0))
                throw new InvalidOperationException("代理范围不为All时必须至少指定一个ProcessType");
            
            if (range != ActingRange.All && processTypes != null)
                processTypes.ToList().ForEach(o => this._actingItems.Add(new ProcessActingItem(o)));

            this.Validate();
        }

        /// <summary>
        /// 判断代理内容是否有效
        /// </summary>
        /// <param name="toActing"></param>
        /// <returns></returns>
        public virtual bool CheckValid(ProcessType toActing)
        {
            return toActing != null
                && this.IsValid
                && (this.Range == ActingRange.All
                || this.ActingItems.FirstOrDefault(o => o.ProcessTypeName == toActing.Name) != null);
        }

        private void ChangeTime(DateTime begin, DateTime end)
        {
            this.BeginTime = begin;
            this.EndTime = end;

            if (this.BeginTime > this.EndTime)
                throw new InvalidOperationException("Begin不能大于End");
        }

        private void Validate()
        {
            if (this.User == null)
                throw new InvalidOperationException("User不能为空");
            if (this.ActAs == null)
                throw new InvalidOperationException("ActAs不能为空");
            if (this.ActAs.UserName == this.User.UserName)
                throw new InvalidOperationException("不能将自己设置为代理人");
        }
    }
}