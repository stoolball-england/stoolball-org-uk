# Configuration

## Managing secrets in config files

`*.config` files sometimes need to contain secrets. You can use XDT transforms to add these to the relevant config file at deploy time using the [Umbraco Cloud per-environment naming convention](https://our.umbraco.com/documentation/Umbraco-Cloud/Set-Up/Config-Transforms/).

Begin the filename with `Secret-` (for example `Secret-MyPassword.{config file name}.{environment}.xdt.config`) and commit it to the `.UmbracoCloud` repository, which is private. It will be copied into this application when you build.
