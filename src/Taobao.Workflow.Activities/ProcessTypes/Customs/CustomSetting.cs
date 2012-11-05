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
    /// 自定义节点设置信息
    /// <remarks>
    /// 对应Custom Activity
    /// 支持StartRule、FinishRule
    /// </remarks>
    /// </summary>
    public abstract class CustomSetting : ActivitySetting
    {
        /// <summary>
        /// 获取开始规则，可能为空
        /// </summary>
        public virtual StartRule StartRule { get; private set; }
        /// <summary>
        /// 获取完成规则，可能为空
        /// </summary>
        public virtual FinishRule FinishRule { get; private set; }

        protected CustomSetting() { }
        public CustomSetting(int flowNodeIndex
            , string activityName
            , StartRule startRule
            , FinishRule finishRule
            , bool isChildOfActivity)
            : base(flowNodeIndex
            , activityName
            , isChildOfActivity)
        {
            this.StartRule = startRule;
            this.FinishRule = finishRule;

            this.Validate();
        }

        private void Validate()
        {
           
        }
    }
}
