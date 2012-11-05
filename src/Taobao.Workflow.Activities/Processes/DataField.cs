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
    /// 流程数据字段
    /// </summary>
    public class ProcessDataField : EntityBase<Guid>
    {
        /// <summary>
        /// 获取数据字段的名称
        /// </summary>
        public virtual string Name { get; private set; }
        /// <summary>
        /// 获取或设置数据字段的值
        /// </summary>
        public virtual string Value { get; set; }

        protected ProcessDataField() { }
        /// <summary>
        /// 初始化字段
        /// </summary>
        /// <param name="fieldId"></param>
        /// <param name="name"></param>
        protected internal ProcessDataField(string name)
            : this()
        {
            this.Name = name;

            this.Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrEmpty(this.Name))
                throw new InvalidOperationException("Name不能为空");
            //if (this.FieldId <= 0)
            //    throw new InvalidOperationException("FieldId不合法");
        }
    }
}