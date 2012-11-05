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
    /*
     * 流程状态的调度以及各类流程操作的依据，状态变迁必须有严谨依据和并发考虑
     * HACK:Active|Stopped|Error状态对于调度而言是属于安全状态，只有处于这些状态的流程才能进行调度级别的调整操作，如：Goto,Retry,ChangeType等
     * 由于延迟调度的存在，流程始终处于running，在合理时间间隔内可以认为是调度安全，因此对一些维护性操作需要考虑此情况
     * PorcessService提供IsSchedulingSafe用于判断此类情况
     *
     */

    /// <summary>
    /// 流程状态
    /// </summary>
    public enum ProcessStatus
    {
        /// <summary>
        /// 新建
        /// </summary>
        New = 0,
        /// <summary>
        /// 发生错误
        /// <remarks>
        /// 此时通常是严重异常或系统异常导致，无法细粒度重试
        /// 也可能不处于此状态的流程仍存在局部异常
        /// </remarks>
        /// </summary>
        Error = 1,
        /// <summary>
        /// 运行中，表示工作流正在等待或被调度
        /// <remarks>
        /// 此时存在对应调度项
        /// 处于该状态的流程通常不允许对其进行操作
        /// </remarks>
        /// </summary>
        Running = 2,
        /// <summary>
        /// 活动，表示工作流正常进入idle，如：存在激活的用户活动（任务）或活动的子流程等，此时不存在需要执行的调度项，允许对流程进行操作
        /// </summary>
        Active = 3,
        /// <summary>
        /// 完成
        /// <remarks>正常流转结束</remarks>
        /// </summary>
        Completed = 4,
        /// <summary>
        /// 被停止
        /// </summary>
        Stopped = 5,
        /// <summary>
        /// 被删除
        /// <remarks>被删除或取消</remarks>
        /// </summary>
        Deleted = 6
    }
}