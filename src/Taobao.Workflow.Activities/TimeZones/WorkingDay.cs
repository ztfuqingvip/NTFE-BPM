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
using System.Collections;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 工作具体设置
    /// </summary>
    public class WorkingDay : EntityBase<Guid>
    {
        private static readonly CodeSharp.Core.Utils.Serializer _serializer = new CodeSharp.Core.Utils.Serializer();

        /// <summary>
        /// 获取时区标识
        /// </summary>
        public virtual Guid TimeZoneId { get; private set; }
        /// <summary>
        /// 获取或设置描述
        /// </summary>
        public virtual string Description { get; set; }
        /// <summary>
        /// 获取星期
        /// </summary>
        public virtual DayOfWeek? DayOfWeek { get; private set; }
        /// <summary>
        /// 工作类型
        /// </summary>
        public virtual WorkingType WorkingType 
        { 
            get 
            {
                if (this.DayOfWeek != null)
                    return WorkingType.WeeklyDay;
                else if (this.Date != null)
                    if (this.WorkingHours != null)
                        return WorkingType.SpecialDay;
                    else
                        return WorkingType.ExceptionDay;
                else
                    throw new InvalidOperationException("无法辨别类型");
            } 
        }

        private string _hours { get; set; }

        private List<WorkingHour> _workHours 
        {
            get
            {
                return _serializer.JsonDeserialize<List<WorkingHour>>(this._hours); 
            }
            set
            {
                this._hours = _serializer.JsonSerialize(value);
            }
        }
        /// <summary>
        /// 时间范围
        /// </summary>
        public virtual IEnumerable<WorkingHour> WorkingHours { get { return this._workHours.AsEnumerable(); } }
        /// <summary>
        /// 特别声明/例外的日期
        /// </summary>
        public virtual DateTime? Date { get; private set; }

        protected WorkingDay() { }

        public WorkingDay(DayOfWeek? dayOfWeek
            , List<WorkingHour> hours
            , DateTime? date)
        {
            this.DayOfWeek = dayOfWeek;
            this._workHours = hours;
            this.Date = date;
            this.Validate();
        }

        /// <summary>
        /// 添加工作时
        /// </summary>
        /// <param name="workingHours"></param>
        protected internal virtual void AddWorkingHours(List<WorkingHour> workingHours)
        {
            this._workHours = this._workHours.ToList().Union(workingHours, new WorkingHourComparer()).OrderBy(o => o.From).ToList();
        }
        /// <summary>
        /// 移除工作时
        /// </summary>
        /// <param name="workingHours"></param>
        protected internal virtual void RemoveWorkingHours(List<WorkingHour> workingHours)
        {
            this._workHours.RemoveAll(o => workingHours.Exists(p => p.From == o.From && p.To == o.From));
        }

        private void Validate()
        {
            if (this.DayOfWeek == null
                && this.WorkingHours == null 
                && this.Date == null)
                throw new InvalidOperationException("DayOfWeek,Hours,Date不能同时为空");
        }
    }
    
    /// <summary>
    /// 工作时比较器
    /// </summary>
    public class WorkingHourComparer : IEqualityComparer<WorkingHour>
    {
        public bool Equals(WorkingHour x, WorkingHour y)
        {
            if(x.From == y.From 
                && x.To == y.To)
                return true;
            return false;
        }
        public int GetHashCode(WorkingHour obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
}
