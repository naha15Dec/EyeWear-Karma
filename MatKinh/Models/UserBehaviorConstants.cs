namespace MatKinh.Models
{
    public static class UserBehaviorConstants
    {
        public const string VIEW = "VIEW";
        public const string ADD_TO_CART = "ADD_TO_CART";
        public const string PURCHASE = "PURCHASE";

        public const decimal VIEW_WEIGHT = 1m;
        public const decimal ADD_TO_CART_WEIGHT = 3m;
        public const decimal PURCHASE_WEIGHT = 5m;
    }
}