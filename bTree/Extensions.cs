public static class Extensions
{
    // Получить средне-арифметическое значение всех внутренних узлов
    public static int GetArithmeticalMean(this BSTree bTree)
    {
        if (bTree.Root == null)
        {
            return 0;
        }

        int countNodes = 0;
        int summ = calcAvg(bTree.Root, ref countNodes);
        // System.Console.WriteLine("countNodes:{0} - summ:{1}", countNodes, summ);
        return summ / countNodes;
    }

    // рекурсивно погружаюсь внутрь дерева и считаю сумму 
    public static int calcAvg(BNode node, ref int countNodes)
    {
        if (node == null)
        {
            return 0;
        }

        // Если узел не внутренний, то мы его пропускаем в подсчетах
        if (node.IsInternal() == false)
        {
            return 0;
        }
        
        countNodes++;
        return (node.Value + calcAvg(node.LeftNode, ref countNodes) + calcAvg(node.RightNode, ref countNodes));
    }
    public static bool IsInternal(this BNode node)
    {
        // если у узла есть потомок то это внутренний узел
        return (node.LeftNode != null || node.RightNode != null);
    }
}