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
    /// 流程代理项
    /// </summary>
    public class ProcessActingItem : EntityBase<Guid>
    {
        /// <summary>
        /// 代理的流程类型名
        /// </summary>
        public virtual string ProcessTypeName { get; private set; }

        protected ProcessActingItem() { }

        public ProcessActingItem(ProcessType type)
        {
            if (type == null)
                throw new InvalidOperationException("ProcessType不能为空");

            this.ProcessTypeName = type.Name;
        }

    }
}
