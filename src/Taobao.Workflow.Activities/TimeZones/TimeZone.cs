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
    /// 时区设置信息
    /// </summary>
    public class TimeZone : EntityBase<Guid>, IAggregateRoot
    {
        /// <summary>
        /// 获取时区名称
        /// </summary>
        public virtual string Name { get; private set; }
        /// <summary>
        /// 获取时区描述
        /// </summary>
        public virtual string Description { get; set; }
        /// <summary>
        /// 获取时区创建日期
        /// </summary>
        public virtual DateTime CreateTime { get; private set; }

        private IList<WorkingDay> _days { get; set; }
        /// <summary>
        /// 获取工作具体设置
        /// </summary>
        public virtual IEnumerable<WorkingDay> WorkingDays { get { return this._days.AsEnumerable();  } }

        protected TimeZone()
        {
            this.CreateTime = DateTime.Now;

            this._days = new List<WorkingDay>();
        }
        public TimeZone(string name)
            : this()
        {
            this.Name = name;
            this.Validate();
        }

        /// <summary>
        /// 添加工作时间范围
        /// </summary>
        /// <param name="dayOfWeek">星期</param>
        /// <param name="fromHour">起始小时，如：9点</param>
        /// <param name="toHour">结束小时，如：18点</param>
        public virtual void AddRangeTime(DayOfWeek dayOfWeek, double fromHour, double toHour)
        {
            this.ValidateWorkingHour(fromHour, toHour);

            var workingHours = new List<WorkingHour>();
            for (var index = fromHour; index < toHour; index += 0.5)
                workingHours.Add(new WorkingHour() { From = index, To = index + 0.5 });

            var workingDay = this._days.FirstOrDefault(o => o.WorkingType == WorkingType.WeeklyDay 
                && o.DayOfWeek == dayOfWeek);
            if (workingDay == null)
                this._days.Add(new WorkingDay(dayOfWeek, workingHours, null));
            else
                workingDay.AddWorkingHours(workingHours);
        }
        /// <summary>
        /// 添加特别声明的日期以及时间范围
        /// </summary>
        /// <param name="date"></param>
        /// <param name="fromHour"></param>
        /// <param name="toHour"></param>
        public virtual void AddSpecialDay(DateTime date, double fromHour, double toHour)
        { 
            this.ValidateWorkingHour(fromHour, toHour);

            var workingHours = new List<WorkingHour>();
            workingHours.Add(new WorkingHour() { From = fromHour, To = toHour });

            var workingDay = this._days.FirstOrDefault(o => o.WorkingType == WorkingType.SpecialDay
                && o.Date.Value.Date == date.Date);
            if (workingDay == null)
                this._days.Add(new WorkingDay(null, workingHours, date.Date));
            else
                workingDay.AddWorkingHours(workingHours);
        }
        /// <summary>
        /// 添加例外的日期
        /// </summary>
        /// <param name="date"></param>
        public virtual void AddExceptionDay(DateTime date)
        {
            var workingDay = this._days.FirstOrDefault(o => o.WorkingType == WorkingType.ExceptionDay
                && o.Date.Value.Date == date.Date);
            if(workingDay == null)
                this._days.Add(new WorkingDay(null, null, date.Date));
        }
        /// <summary>
        /// 工作时间范围
        /// </summary>
        /// <param name="dayOfWeek"></param>
        /// <param name="fromHour"></param>
        /// <param name="toHour"></param>
        public virtual void RemoveRangeTime(DayOfWeek dayOfWeek, double fromHour, double toHour)
        {
            this.ValidateWorkingHour(fromHour, toHour);

            var workingHours = new List<WorkingHour>();
            for (var index = fromHour; index < toHour; index += 0.5)
                workingHours.Add(new WorkingHour() { From = index, To = index + 0.5 });

            var workingDay = this._days.FirstOrDefault(o => o.WorkingType == WorkingType.WeeklyDay
                && o.DayOfWeek == dayOfWeek);
            if (workingDay != null)
                workingDay.RemoveWorkingHours(workingHours);
        }
        /// <summary>
        /// 基于该工作时设置计算从当前开始经过间隔时间后的实际时间
        /// </summary>
        /// <param name="after"></param>
        /// <returns></returns>
        public virtual DateTime CalculateDateTime(TimeSpan after)
        {
            var now = DateTime.Now;
            //TODO:重构计算逻辑
            //var now = DateTime.Now.AddMinutes(after.TotalMinutes);
            //for (int index = 0; index < 366; index++)
            //{
            //    var tempNowForSpecialDay = DateTime.MaxValue;
            //    //获取例外日期
            //    var exceptionDay = this._days.Where(o => o.WorkingType == WorkingType.ExceptionDay
            //        && o.Date == now.Date).FirstOrDefault();
            //    if (exceptionDay != null)
            //    {
            //        now = now.Date.AddDays(1);
            //        continue;
            //    }
            //    //获取特别声明日期
            //    var specialDay = this._days.Where(o => o.WorkingType == WorkingType.SpecialDay
            //        && o.Date == now.Date).FirstOrDefault();
            //    if (specialDay != null)
            //        foreach (var workingHour in specialDay.WorkingHours)
            //            if (now.Hour + now.Minute / 60.0 <= workingHour.To)
            //            {
            //                tempNowForSpecialDay = now.Hour + now.Minute / 60.0 >= workingHour.From
            //                    ? now : now.Date + TimeSpan.FromHours(workingHour.From);
            //                break;
            //            }
            //    var tempNowForWeeklyDay = DateTime.MaxValue;
            //    //获取工作周期日期
            //    var weeklyDay = this._days.Where(o => o.WorkingType == WorkingType.WeeklyDay
            //        && o.DayOfWeek == now.Date.DayOfWeek).FirstOrDefault();
            //    if (weeklyDay != null)
            //        foreach (var workingHour in weeklyDay.WorkingHours)
            //            if (now.Hour + now.Minute / 60.0 <= workingHour.To)
            //            {
            //                tempNowForWeeklyDay = now.Hour + now.Minute / 60.0 >= workingHour.From
            //                    ? now : now.Date + TimeSpan.FromHours(workingHour.From);
            //                break;
            //            }
            //    now = tempNowForSpecialDay == DateTime.MaxValue && tempNowForWeeklyDay == DateTime.MaxValue
            //        ? now.Date.AddDays(1) : (tempNowForSpecialDay < tempNowForWeeklyDay ? tempNowForSpecialDay : tempNowForWeeklyDay);
            //}
            return now;
        }

        private void Validate()
        {
            if (string.IsNullOrEmpty(this.Name))
                throw new InvalidOperationException("Name不能为空");
        }
        private void ValidateWorkingHour(double fromHour, double toHour)
        {
            if (fromHour >= toHour)
                throw new InvalidOperationException("起始时间不能大于结束时间");
            if ((fromHour * 60) % 30 != 0 
                || (toHour * 60) % 30 != 0)
                throw new InvalidOperationException("时间必须以小时数0.5为单位间隔");
        }

        //private IList<WorkingWeeklyDay> _weeklyDays;
        //private IList<SpecialDay> _specialDays;
    }

    ///// <summary>
    ///// 工作日
    ///// </summary>
    //public class WorkingWeeklyDay : EntityBase<Guid>
    //{
    //    /// <summary>
    //    /// 获取星期
    //    /// </summary>
    //    public virtual DayOfWeek DayOfWeek { get; private set; }

    //    private IList<WorkingHour> _hours { get; set; }
    //}
    ///// <summary>
    ///// 工作时
    ///// </summary>
    //public class WorkingHour
    //{
    //    /// <summary>
    //    /// 获取起始时间点，如：9点
    //    /// </summary>
    //    public virtual double From { get; private set; }
    //    /// <summary>
    //    /// 获取结束时间点，如：18点
    //    /// </summary>
    //    public virtual double To { get; private set; }

    //    public WorkingHour(double from, double to)
    //    {
    //        this.From = from;
    //        this.To = to;
    //    }
    //}
    ///// <summary>
    ///// 特别声明的日期
    ///// </summary> 
    //public class SpecialDay : EntityBase<Guid>
    //{
    //    public virtual DateTime Date { get; private set; }
    //    private IList<WorkingHour> _hours { get; set; }

    //    public bool IsWorkingDate { get; set; }
    //}
}