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
    /// 用户信息
    /// </summary>
    public class User : EntityBase<Guid>, IAggregateRoot
    {
        /// <summary>
        /// 获取用户名
        /// </summary>
        public virtual string UserName { get; private set; }
        /// <summary>
        /// 获取用户创建时间
        /// </summary>
        public virtual DateTime CreateTime { get; private set; }

        protected User()
        {
            this.CreateTime = DateTime.Now;
        }
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="userName"></param>
        public User(string userName)
            : this()
        {
            this.UserName = userName;

            this.Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrEmpty(this.UserName))
                throw new InvalidOperationException("UserName不能为空");
        }
    }
}