using System;
using System.IO;
using System.Text.RegularExpressions;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Common;
using Umbraco.Cms.Infrastructure.Migrations.Expressions.Execute;

namespace Stoolball.Data.UmbracoMigrations
{
    internal static class IExecuteBuilderExtensions
    {
        internal static IExecutableBuilder SqlFromFile(this IExecuteBuilder builder, string folderAndFilename)
        {
            if (Regex.IsMatch(folderAndFilename, "^[0-9]")) { folderAndFilename = "_" + folderAndFilename; }

            using var stream = typeof(IExecuteBuilderExtensions).Assembly.GetManifestResourceStream($"Stoolball.Data.UmbracoMigrations.{folderAndFilename}");
            using var reader = new StreamReader(stream!);
            var sql = reader.ReadToEnd();

            sql = sql.Replace("@", "@@"); // parameters must be escaped, or they will be treated as expected parameters for the CREATE statement
            sql = Regex.Replace(sql, Environment.NewLine, " " + Environment.NewLine); // Umbraco concatenates everything onto one line, so ensure there's a space at the end to avoid combining with keywords on the next line
            sql = Regex.Replace(sql, "--.*", string.Empty); // Umbraco concatenates everything onto one line, so remove comments that would also comment out any following code

            return builder.Sql(sql);
        }
    }
}
