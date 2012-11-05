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

namespace Taobao.Workflow.Activities.Application
{
    /// <summary>
    /// 为Taobao.Activities实现的日志工厂
    /// </summary>
    [CodeSharp.Core.Component(CodeSharp.Core.LifeStyle.Singleton)]
    public class LoggerFactory : Taobao.Activities.Interfaces.ILoggerFactory
    {
        private CodeSharp.Core.Services.ILoggerFactory _factory;

        public LoggerFactory(CodeSharp.Core.Services.ILoggerFactory factory)
        {
            this._factory = factory;
        }

        #region ILoggerFactory Members

        public Taobao.Activities.Interfaces.ILog Create(string name)
        {
            return new Logger(this._factory.Create(name));
        }

        public Taobao.Activities.Interfaces.ILog Create(Type type)
        {
            return new Logger(this._factory.Create(type));
        }

        #endregion
    }
}
