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
    /// 人工节点的超时事件升级规则
    /// </summary>
    public class HumanEscalationRule
    {
        /// <summary>
        /// 获取超时时间间隔
        /// </summary>
        public virtual double? ExpirationMinutes { get; private set; }
        /// <summary>
        /// 获取使用的时区
        /// </summary>
        public virtual TimeZone TimeZone { get; private set; }

        #region 通知规则
        /// <summary>
        /// 获取重复通知时间间隔
        /// </summary>
        public virtual double? NotifyRepeatMinutes { get; private set; }
        /// <summary>
        /// 获取消息通知模板名称
        /// </summary>
        public virtual string NotifyTemplateName { get; private set; }
        #endregion

        #region 节点流转规则
        /// <summary>
        /// 获取流转到的节点的名称
        /// </summary>
        public virtual string GotoActivityName { get; private set; }
        #endregion

        #region 任务转交规则
        /// <summary>
        /// 获取任务转交人目标用户
        /// <remarks>支持流程变量</remarks>
        /// </summary>
        public virtual string RedirectTo { get; private set; }
        #endregion

        /// <summary>
        /// 获取规则是否有效
        /// </summary>
        public virtual bool IsValid { get { return this.ExpirationMinutes.HasValue && this.ExpirationMinutes > 0; } }

        protected HumanEscalationRule() { }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="expirationMinutes"></param>
        /// <param name="timeZone"></param>
        protected HumanEscalationRule(double? expirationMinutes, TimeZone timeZone)
            : this()
        {
            this.ExpirationMinutes = expirationMinutes;
            this.TimeZone = timeZone;
        }
        /// <summary>
        /// 创建超时事件升级规则
        /// </summary>
        /// <param name="expirationMinutes">超时时间</param>
        /// <param name="timeZone">时区</param>
        /// <param name="notifyRepeatMinutes">消息重复通知的间隔时间</param>
        /// <param name="notifyTemplateName">通知时使用的模板名</param>
        /// <param name="gotoActivityName">跳转的节点名称</param>
        /// <param name="redirectTo">转交人，支持变量脚本解析</param>
        public HumanEscalationRule(double? expirationMinutes
            , TimeZone timeZone
            , double? notifyRepeatMinutes
            , string notifyTemplateName
            , string gotoActivityName
            , string redirectTo)
            : this(expirationMinutes
            , timeZone)
        {
            this.NotifyRepeatMinutes = notifyRepeatMinutes;
            this.NotifyTemplateName = notifyTemplateName;
            this.GotoActivityName = gotoActivityName;
            this.RedirectTo = redirectTo;
        }
    }
}