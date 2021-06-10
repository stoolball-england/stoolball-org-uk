# Caching

Connecting to the database is the both biggest performance bottleneck and the biggest source of errors, therefore caching content rather than fetching it from the database every time makes the site both faster and more reliable.

Umbraco content is cached in memory by default. Stoolball data is cached using [read-through caching using Polly](https://github.com/App-vNext/Polly/wiki/Cache).

## Cache strategies

There are three strategies used for caching:

- **Update for all when cache expires**

  Suitable for data that's not too time-sensitive, and is typically generated from other data. Updates may not be visible on the website immediately. This is used for:

  - stoolball statistics

- **Update immediately for the editor's account**

  Suitable for data that's not too time-sensitive, but is directly editable. If you update some data you want to see that change take effect so that you know it worked. In this strategy the cache is invalidated immediately for the account who made the update, but other accounts and users who are not signed in will see the update when the cache naturally expires. This is used for:

  - club and team listings

- **Update immediately for all**

  Suitable for data that's time-sensitive and directly editable. If you update some data that people might need to see straight away, such as cancelling a match, the cache is invalidated immediately for all users. This is used for:

  - match and tournament listings
  - match and tournament comments

## How it's implemented

Each data source in the `Stoolball.Data.SqlServer` project that supports caching has a matching data source in `Stoolball.Data.Cache`. Both implement an interface like `IExampleDataSource` but the SQL Server version also implements `ICacheableExampleDataSource`. The cached data source can then inject the SQL Server data source as a dependency, allowing it to read data from the cache if it's available, or fall back to reading from SQL Server if not.

In `DependencyInjectionComposer.cs` the application is configured to inject the cached data source into any class that requires an `IExampleDataSource`, and the SQL Server data source for any class that requires an `ICacheableExampleDataSource`. The consuming class does not need to know whether the data is cached, and caching can be completely disabled for testing by switching the registration of `IExampleDataSource` in `DependencyInjectionComposer.cs` to inject the SQL Server data source.

The cached data sources each use a Polly cache policy which is also defined in `DependencyInjectionComposer.cs`, and this defines the length of time the cache lasts for, and whether it uses absolute or sliding expiration.

A similar pattern is used for `CacheClearing*Repository.cs` classes for matches and tournaments to make the cache aware of updates which require the cache to be cleared for some or all users. Team listings handle this differently by requiring an `ICacheOverride` in the controller and calling a method on that to override the cache.

## Further caching with Examine

In-memory caching will always be the best for performance, but it will also always need an expiration date that requires a new request to the database. Reading and writing entities from Examine is a potential future middle layer that offers a more permanent cache, which can be updated only when entities are updated, and read quickly without needing to connect to the database.
