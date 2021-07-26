using System;

namespace Stoolball
{
    public interface IUrlFormatter
    {
        Uri PrefixHttpsProtocol(string url);
    }
}