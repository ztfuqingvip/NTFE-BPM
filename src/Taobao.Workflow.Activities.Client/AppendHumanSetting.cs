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

namespace Taobao.Workflow.Activities.Client
{
    /// <summary>
    /// 描述动态追加人工节点时使用的设置信息
    /// </summary>
    public class AppendHumanSetting
    {
        /// <summary>
        /// 从当前节点进入动态节点时使用的任务操作（Action）名称
        /// <remarks>即在ExecuteWorkItem时提供的Action</remarks>
        /// </summary>
        public string EnterAction { get; set; }
        /// <summary>
        /// 从当前节点进入动态节点时使用的完成规则名称
        /// <remarks>即按EnterAction执行任务后，激活的完成规则名称</remarks>
        /// </summary>
        public string EnterFinishRuleName { get; set; }

        /// <summary>
        /// 节点名称
        /// </summary>
        public string ActivityName { get; set; }
        /// <summary>
        /// 可执行动作定义
        /// </summary>
        public string[] Actions { get; set; }
        /// <summary>
        /// 人工任务页面地址
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 执行人规则
        /// </summary>
        public string[] ActionerRule { get; set; }
        /// <summary>
        /// slot数量
        /// <remarks>若为-1则忽略</remarks>
        /// </summary>
        public int SlotCount { get; set; }
        /// <summary>
        /// Slot分发模式
        /// <remarks>默认为一次性分发</remarks>
        /// </summary>
        public SlotDistributionMode SlotMode { get; set; }

        /// <summary>
        /// 完成规则名称
        /// </summary>
        public string FinishRuleName { get; set; }
        /// <summary>
        /// 完成规则内容
        /// </summary>
        public string FinishRuleBody { get; set; }
    }
    /// <summary>
    /// 动态追加人工节点模式
    /// </summary>
    public enum AppendHumanMode
    {
        /// <summary>
        /// 新节点完成后继续下一步
        /// </summary>
        Continues,
        /// <summary>
        /// 等待新节点完成后返回到原节点
        /// </summary>
        Wait
    }
    /// <summary>
    /// Slot的分发模式
    /// </summary>
    public enum SlotDistributionMode
    {
        /// <summary>
        /// 一次性分发 
        /// </summary>
        AllAtOnce = 0,
        /// <summary>
        /// 按顺序分发
        /// </summary>
        OneAtOnce = 1
    }
}