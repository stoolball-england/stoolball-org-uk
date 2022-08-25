namespace Stoolball.Routing
{
    public class BestRouteSelector : IBestRouteSelector
    {
        private readonly IRouteTokeniser _tokeniser;

        public BestRouteSelector(IRouteTokeniser tokeniser)
        {
            _tokeniser = tokeniser ?? throw new System.ArgumentNullException(nameof(tokeniser));
        }

        public string SelectBestRoute(string route1, string route2)
        {
            var tokenised1 = _tokeniser.TokeniseRoute(route1);
            var tokenised2 = _tokeniser.TokeniseRoute(route2);

            if (tokenised1.counter.HasValue && tokenised2.counter.HasValue)
            {
                if (tokenised1.counter.Value == tokenised2.counter.Value)
                {
                    return tokenised1.baseRoute.Length > tokenised2.baseRoute.Length ? route1 : route2;
                }
                else
                {
                    return tokenised1.counter > tokenised2.counter ? route2 : route1;
                }
            }
            else if (!tokenised1.counter.HasValue && tokenised2.counter.HasValue)
            {
                return route1;
            }
            else if (tokenised1.counter.HasValue && !tokenised2.counter.HasValue)
            {
                return route2;
            }
            else
            {
                return route1.Length > route2.Length ? route1 : route2;
            }
        }
    }
}
