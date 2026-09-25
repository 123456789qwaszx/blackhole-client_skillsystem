using System;

namespace BlackHole.Core
{
    public enum PurchaseResult { Purchased, InBattle, AlreadyOwned, MissingPrerequisite, NotEnoughGold, UnknownNode }
    public enum UpgradeNodeState { Hidden, Revealed, Purchasable, Owned }

    // 그래프는 공개 여부를, 구매는 전투 상태·보유·가격을 판단한다. 실패는 상태를 바꾸지 않는다.
    public static class UpgradePurchase
    {
        public static PurchaseResult Check(PlayerState state, GameContent content, string nodeId)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (content == null) throw new ArgumentNullException(nameof(content));
            if (!content.TryGetUpgrade(nodeId, out UpgradeNodeDefinition node)) return PurchaseResult.UnknownNode;
            if (state.InBattle) return PurchaseResult.InBattle;
            if (state.Owns(nodeId)) return PurchaseResult.AlreadyOwned;
            if (!content.Graph.IsRevealed(nodeId, state)) return PurchaseResult.MissingPrerequisite;
            if (state.Gold < node.Price) return PurchaseResult.NotEnoughGold;
            return PurchaseResult.Purchased;
        }

        public static UpgradeNodeState GetState(PlayerState state, GameContent content, string nodeId)
        {
            PurchaseResult result = Check(state, content, nodeId);
            if (result == PurchaseResult.UnknownNode) throw new ArgumentException($"없는 노드 '{nodeId}'.");
            if (state.Owns(nodeId)) return UpgradeNodeState.Owned;
            if (!content.Graph.IsRevealed(nodeId, state)) return UpgradeNodeState.Hidden;
            return result == PurchaseResult.Purchased ? UpgradeNodeState.Purchasable : UpgradeNodeState.Revealed;
        }

        public static PurchaseResult TryPurchase(PlayerState state, GameContent content, string nodeId)
        {
            PurchaseResult result = Check(state, content, nodeId);
            if (result == PurchaseResult.Purchased)
            {
                content.TryGetUpgrade(nodeId, out UpgradeNodeDefinition node);
                state.Buy(node);
            }
            return result;
        }
    }
}
