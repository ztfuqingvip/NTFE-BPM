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
    /// 节点的开始规则
    /// </summary>
    public class StartRule
    {
        /// <summary>
        /// 获取开始时间
        /// </summary>
        public virtual DateTime? At { get; private set; }
        /// <summary>
        /// 获取开始间隔时间
        /// </summary>
        public virtual double? AfterMinutes { get; private set; }
        /// <summary>
        /// 获取时区设置
        /// </summary>
        public virtual TimeZone TimeZone { get; private set; }
        /// <summary>
        /// 获取该规则设置是否有效
        /// </summary>
        public virtual bool IsValid { get { return this.At.HasValue || this.AfterMinutes.HasValue; } }

        protected StartRule() { }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="at"></param>
        /// <param name="afterMinutes"></param>
        /// <param name="timeZone"></param>
        public StartRule(DateTime? at, double? afterMinutes, TimeZone timeZone)
        {
            this.At = at;
            this.AfterMinutes = afterMinutes;
            this.TimeZone = timeZone;
        }

        /// <summary>
        /// 获取实际开始时间
        /// </summary>
        /// <returns></returns>
        public virtual DateTime? GetStartTime()
        {
            if (!this.IsValid)
                return null;
            if (this.At.HasValue)
                return this.At;
            return this.TimeZone == null
                ? DateTime.Now.AddMinutes(this.AfterMinutes.Value)
                : this.TimeZone.CalculateDateTime(TimeSpan.FromMinutes(this.AfterMinutes.Value));
        }

        /// <summary>
        /// 声明无限期延迟的开始规则
        /// </summary>
        /// <returns></returns>
        public static StartRule UnlimitedDelay()
        {
            return new StartRule(DateTime.MaxValue, null, null);
        }
    }
}
