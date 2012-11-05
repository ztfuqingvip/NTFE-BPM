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

using Castle.Services.Transaction;

using CodeSharp.Core.RepositoryFramework;

namespace Taobao.Workflow.Activities
{
    /// <summary>
    /// 流程类型服务对外接口
    /// </summary>
    public interface IProcessTypeService
    {
        /// <summary>
        /// 创建流程类型并设为当前版本
        /// </summary>
        /// <param name="type"></param>
        void Create(ProcessType type);
        /// <summary>
        /// 创建流程类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="current">指示是否设置为当前版本</param>
        void Create(ProcessType type, bool current);
        /// <summary>
        /// 获取流程类型
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        ProcessType GetProcessType(string name);
        /// <summary>
        /// 获取流程类型
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <returns></returns>
        ProcessType GetProcessType(string name, string version);
        /// <summary>
        /// 获取流程类型历史发布信息
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IEnumerable<ProcessType> GetHistories(string name);
        /// <summary>
        /// 获取所有流程类型的当前版本
        /// </summary>
        /// <returns></returns>
        IEnumerable<ProcessType> GetProcessTypes();
        /// <summary>
        /// 将指定版本设置为当前版本
        /// </summary>
        /// <param name="name"></param>
        /// <param name="version"></param>
        void SetAsCurrent(string name, string version);
    }
    /// <summary>
    /// 流程类型服务
    /// </summary>
    [Transactional]
    public class ProcessTypeService : IProcessTypeService
    {
        private static IProcessTypeRepository _repository;
        static ProcessTypeService()
        {
            _repository = RepositoryFactory.GetRepository<IProcessTypeRepository, Guid, ProcessType>();
        }

        #region IProcessTypeService Members
        [Transaction(TransactionMode.Requires)]
        void IProcessTypeService.Create(ProcessType type)
        {
            (this as IProcessTypeService).Create(type, true);
        }
        [Transaction(TransactionMode.Requires)]
        void IProcessTypeService.Create(ProcessType type, bool current)
        {
            type.IsCurrent = current;
            _repository.Add(type);
            if (current)
                (this as IProcessTypeService).SetAsCurrent(type.Name, type.Version);
        }
        ProcessType IProcessTypeService.GetProcessType(string name)
        {
            return _repository.FindCurrentProcessType(name);
        }
        ProcessType IProcessTypeService.GetProcessType(string name, string version)
        {
            return _repository.FindByVersion(name, version);
        }
        IEnumerable<ProcessType> IProcessTypeService.GetHistories(string name)
        {
            return _repository.FindHistories(name);
        }
        IEnumerable<ProcessType> IProcessTypeService.GetProcessTypes()
        {
            return _repository.FindAllCurrent();
        }
        [Transaction(TransactionMode.Requires)]
        void IProcessTypeService.SetAsCurrent(string name, string version)
        {
            var types = _repository.FindAll(name);
            if (types.Count() == 0)
                throw new InvalidOperationException("没有找到名为" + name + "的流程类型信息");
            var target = types.FirstOrDefault(o => o.Version == version);
            if (target == null)
                throw new InvalidOperationException("没有找到版本为" + version + "的流程类型" + name);

            types.Where(o => o.IsCurrent && o != target)
                .ToList()
                .ForEach(o =>
                {
                    o.IsCurrent = false;
                    _repository.Update(target);
                });
            target.IsCurrent = true;
            _repository.Update(target);
        }
        #endregion
    }
}