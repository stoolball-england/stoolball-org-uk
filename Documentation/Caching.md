# Caching

Connecting to the database is the both biggest performance bottleneck and the biggest source of errors, therefore caching content rather than fetching it from the database every time makes the site both faster and more reliable.

Umbraco content is cached in memory by default. Stoolball data is cached using read-through caching, mostly [read-through caching using Polly](https://github.com/App-vNext/Polly/wiki/Cache).

## Cache strategies

There are two strategies used for caching:

- **Update for all when cache expires**

  Suitable for data that's not too time-sensitive, and is typically generated from other data. Updates may not be visible on the website immediately. This is used for:

  - stoolball statistics

- **Update immediately for all**

  Suitable for data that's time-sensitive and directly editable. The cache is invalidated immediately for all users. If you update some data you want to see that change take effect so that you know it worked. If your update is time-sensitive people might need to see it straight away, such as cancelling a match. This is used for:

  - club and team listings
  - competition listings
  - match and tournament listings
  - match and tournament comments
  - match location listings
  - linking a player to a member account

## How it's implemented

Each data source in the `Stoolball.Data.SqlServer` project that supports caching has a matching data source in `Stoolball.Data.Cache`. Both implement an interface like `IExampleDataSource` but the SQL Server version also implements `ICacheableExampleDataSource`. The cached data source can then inject the SQL Server data source as a dependency, allowing it to read data from the cache if it's available, or fall back to reading from SQL Server if not.

In `Startup.cs` the application is configured to inject the cached data source into any class that requires an `IExampleDataSource`, and the SQL Server data source for any class that requires an `ICacheableExampleDataSource`. The consuming class does not need to know whether the data is cached, and caching can be completely disabled for testing by switching the registration of `IExampleDataSource` in `Startup.cs` to inject the SQL Server data source.

The cached data sources define the length of time the cache lasts for, and whether it uses absolute or sliding expiration, using the `CacheConstants` class. In some cases these use a Polly cache policy which is defined in `Startup.cs`.

When caches need to be updated immediately for all the controller either requires an `ICacheClearer<T>` and calls the `ClearCacheFor` method with the object the cache needs to be cleared for, or for listings it requires an `IListingCacheClearer<T>` and a call to `ClearCache`.

## Further caching with Examine

In-memory caching will always be the best for performance, but it will also always need an expiration date that requires a new request to the database. Reading and writing entities from Examine is a potential future middle layer that offers a more permanent cache, which can be updated only when entities are updated, and read quickly without needing to connect to the database.
