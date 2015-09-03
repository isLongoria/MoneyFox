﻿using System;
using System.Threading.Tasks;
using Windows.UI.StartScreen;

namespace MoneyManager.Windows.Tile
{
    public abstract class Tile
    {
        protected bool TileExists(string id)
        {
            return SecondaryTile.Exists(id);
        }

        protected async Task Create(SecondaryTile secondTile)
        {
            secondTile.VisualElements.ShowNameOnSquare150x150Logo = true;
            await secondTile.RequestCreateAsync();
        }

        protected async Task Remove(SecondaryTile secondTile)
        {
            await secondTile.RequestDeleteAsync();
        }

        public static void UpdateMainTile()
        {
            //TODO: Refactor tile stuff
            //var cashFlow = StatisticLogic.GetMonthlyCashFlow();

            //Mvx.Resolve<IUserNotification>()
            //    .UpdateMainTile(cashFlow[0].Label, cashFlow[1].Label, cashFlow[2].Label);
        }
    }
}