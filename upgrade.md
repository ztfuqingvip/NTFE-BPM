# lib upgrade history
====

- 20121105 by wsky

base on castle/nhibernate upgrade record:

https://github.com/codesharp/infrastructure/blob/master/upgrade.md

More:

NH3.x mapping is more strict, conducive to checking the validity of mapping.

1.Component lazyload is not work
```c#
Component<ProcessType.WorkflowDefinition>(...).LazyLoad();
```
should be:
```c#
Component<ProcessType.WorkflowDefinition>(...);
```

2.The length of the string value exceeds the length configured in the mapping/parameter.

https://github.com/ali-ent/NTFE-BPM/issues/1

https://groups.google.com/forum/?fromgroups=#!topic/nhibernate-development/HG7xB0-KR3g

```c#
this.GetSession().CreateSQLQuery().SetString("data", data)
```
occur when data.length is longer than 4000.

should change mapping or sqltype:
http://www.tritac.com/bp-21-fluent-nhibernate-nvarcharmax-fields-truncated-to-4000-characters
```c#
xxx.SetParameter("data", data, NHibernate.NHibernateUtil.StringClob)
```
nvarchar(max) mapping:
```c#
.Column("WorkflowData").CustomType("StringClob")
```
