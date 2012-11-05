using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Taobao.Workflow.Activities.Hosting;
using NHibernate;
using Taobao.Infrastructure;
using Taobao.Infrastructure.Services;
using System.Runtime.Remoting.Messaging;

namespace Taobao.Workflow.Activities.Application
{
    /// <summary>
    /// 提供基于NH以及和Taobao.Infrastructure.Castle结合的简易事务辅助
    /// </summary>
    [Taobao.Infrastructure.Component]
    public class TransactionHelper : Scheduler.ITransactionHelper
    {
        private ILog _log;
        private ISessionFactory _factory;
        public TransactionHelper(ILoggerFactory loggerFactory, ISessionFactory sessionFactory)
        {
            this._log = loggerFactory.Create(typeof(TransactionHelper));
            this._factory = sessionFactory;
        }

        #region ITransactionHelper Members

        Scheduler.TransactionScope Scheduler.ITransactionHelper.Begin()
        {
            return new TransactionScope(this._factory.OpenSession(), this._log);
        }

        #endregion
    }
    /// <summary>
    /// 简易事务区域，动态替换基础设施的仓储对应的当前session并开启事务
    /// </summary>
    public class TransactionScope : Scheduler.TransactionScope
    {
        //UNDONE:耦合于taobao.infrastructure.castles中的设计，需要调整为让基础库支持此类特性
        private static readonly string UnmanagedSessionKey = "____UnmanagedSession";
        private ILog _log;
        private ISession _prev;
        private ISession _session;
        private ITransaction _transation;

        public TransactionScope(ISession session, ILog log)
        {
            this._log = log;
            this._prev = CallContext.GetData(UnmanagedSessionKey) as ISession;
            this._session = session;
            CallContext.SetData(UnmanagedSessionKey, this._session);
            this._transation = this._session.BeginTransaction();
        }

        public override void Commit()
        {
            this._transation.Commit();
        }
        public override void Rollback()
        {
            this._transation.Rollback();
        }
        public override void Dispose()
        {
            try
            {
                this._transation.Dispose();
                (CallContext.GetData(UnmanagedSessionKey) as ISession).Close();
            }
            catch (Exception e)
            {
                this._log.Warn("释放Scheduler.TransactionScope时异常", e);
            }
            finally
            {
                CallContext.SetData(UnmanagedSessionKey, this._prev);
            }
        }
    }
}