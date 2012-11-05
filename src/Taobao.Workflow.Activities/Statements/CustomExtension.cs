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
using Taobao.Activities;

namespace Taobao.Workflow.Activities.Statements
{
    /// <summary>
    /// 自定义节点扩展功能
    /// </summary>
    public class CustomExtension
    {
        private IList<DelayBookmark> _delays;
        private IList<FaultBookmark> _faults;
        private IList<CustomSetting> _settings;
        /// <summary>
        /// 初始化为自定义活动提供的基本扩展
        /// </summary>
        /// <param name="settings"></param>
        public CustomExtension(IList<CustomSetting> settings)
        {
            this._delays = new List<DelayBookmark>();
            this._faults = new List<FaultBookmark>();
            this._settings = settings;
        }
        /// <summary>
        /// 获取节点设置
        /// </summary>
        /// <param name="activityName"></param>
        /// <returns></returns>
        public CustomSetting GetActivitySetting(string activityName)
        {
            return _settings.FirstOrDefault(o => o.ActivityName == activityName);
        }
        /// <summary>
        /// 获取延迟书签信息
        /// </summary>
        /// <returns></returns>
        public IEnumerable<DelayBookmark> GetDelayBookmarks()
        {
            return this._delays;
        }
        /// <summary>
        /// 获取错误书签信息
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FaultBookmark> GetFaultBookmarks()
        {
            return this._faults;
        }
        /// <summary>
        /// 添加延迟信息
        /// </summary>
        /// <param name="context"></param>
        /// <param name="at"></param>
        public void AddDelay(string activityName, string bookmarkName, DateTime at)
        {
            this._delays.Add(new DelayBookmark(activityName, bookmarkName, at));
        }
        /// <summary>
        /// 添加错误信息
        /// </summary>
        /// <param name="activityName"></param>
        /// <param name="bookmarkName"></param>
        /// <param name="reason"></param>
        public void AddFault(string activityName, string bookmarkName, Exception reason)
        {
            this._faults.Add(new FaultBookmark(activityName, bookmarkName, reason));
        }
        /// <summary>
        /// 清除暂存的信息
        /// </summary>
        public void Clear()
        {
            this.ClearDelay();
            this.ClearFault();
        }
        /// <summary>
        /// 清除暂存延迟信息
        /// </summary>
        public void ClearDelay()
        {
            this._delays.Clear();
        }
        /// <summary>
        /// 清除暂存错误信息
        /// </summary>
        public void ClearFault()
        {
            this._faults.Clear();
        }

        /// <summary>
        /// 用于节点延迟的书签
        /// </summary>
        public class DelayBookmark
        {
            /// <summary>
            /// 所在节点名称
            /// </summary>
            public string ActivityName { get; set; }
            /// <summary>
            /// 书签名
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 延迟到指定时间
            /// </summary>
            public DateTime At { get; set; }

            public DelayBookmark(string activityName, string bookmarkName, DateTime at)
            {
                this.ActivityName = activityName;
                this.Name = bookmarkName;
                this.At = at;

                this.Validate();
            }
            private void Validate()
            {
                if (string.IsNullOrWhiteSpace(this.ActivityName))
                    throw new InvalidOperationException("ActivityName不能为空");
                if (string.IsNullOrWhiteSpace(this.Name))
                    throw new InvalidOperationException("Name不能为空");
            }
        }
        /// <summary>
        /// 节点错误书签
        /// </summary>
        public class FaultBookmark
        {
            /// <summary>
            /// 所在节点名称
            /// </summary>
            public string ActivityName { get; set; }
            /// <summary>
            /// 书签名
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 错误原因
            /// </summary>
            public Exception Reason { get; set; }

            public FaultBookmark(string activityName, string bookmarkName, Exception reason)
            {
                this.ActivityName = activityName;
                this.Name = bookmarkName;
                this.Reason = reason;

                this.Validate();
            }
            private void Validate()
            {
                if (string.IsNullOrWhiteSpace(this.ActivityName))
                    throw new InvalidOperationException("ActivityName不能为空");
                if (string.IsNullOrWhiteSpace(this.Name))
                    throw new InvalidOperationException("Name不能为空");
                if (this.Reason == null)
                    throw new InvalidOperationException("Reason不能为空");
            }
        }
    }
}
