# Contributing to the Stoolball England website

## Create or comment on an issue

If you have a feature request or want to report a problem, please create an issue or add your comment to an existing issue. All contributions are welcome.

## Starting a new feature or fix

Before starting, create or comment on an issue you're planning to work on to discuss whether it's something we want to include in the website.

On your fork, run `.\Scripts\Pull-UmbracoCloud.ps1` to pull changes from Umbraco Cloud into your branch before starting new work.

Always work on a feature branch named `feature/xxx` or `fix/xxx` based off of `master`.

## Merge into `master` and push to Umbraco Cloud

Run `.\Scripts\Pull-UmbracoCloud.ps1` again to pull changes from Umbraco Cloud before merging into `master`.

The pull from Umbraco Cloud always gets committed to `master`, so the changes should be there before you merge yours in. This should avoid the remote changes overwriting yours.

You can merge `master` into your `feature/xxx` or `fix/xxx` branch if required.

Once your branch is merged into `master` run `.\Scripts\Push-UmbracoCloud.ps1` to push your changes. This should keep the remote up-to-date and minimise conflicts.

## Submit a pull request

When your changes are ready, create a pull request for your feature branch targeting the `master` branch.
