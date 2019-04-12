using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace MissEgg
{
    /// <summary>
    /// Логика враждебной среды
    /// </summary>
    class TypeAttack
    {

        /// <summary>
        /// Герой
        /// </summary>
        #region
        private static Rectangle hero; // Главный герой;
        public int hpHero = 3; // Кол-во здоровья героя;
        #endregion

        /// <summary>
        /// Босс
        /// </summary>
        #region
        private static Image boss; // Босс;
        public bool statusMoveAttackBoss = false; // Состояние босса после использования супер атаки;
        public int hpBoss = 10; // Кол-во здоровья у босса;
        #endregion

        /// <summary>
        /// Сложность игры
        /// </summary>
        #region
        private bool statusSuperSpawnBomb = false;  //Состояние супер атаки;
        public bool statusHorizontAttack = false; //Состояние горизонтальной атаки атаки;
        private TimeSpan delayAnimationBoss = TimeSpan.FromMilliseconds(300); // Задержки перед анимацией босса;     
        private TimeSpan speedAnimationBossLR = TimeSpan.FromSeconds(1); // Скорость анимации (влево-вправо);
        public TimeSpan speedAnimationBossTB = TimeSpan.FromSeconds(2.5); // Скорость анимации (вверх-вниз);
        private TimeSpan speedSpawnDeffBomb = TimeSpan.FromSeconds(1.15); // Скорость с которой бомбы появляются;
        #endregion

        private MainWindow gameApp; // Окно с игрой;
        private static Canvas mapGame; // Карта;
        public readonly double coorWallsTop; // Координаты пола под героем (Canvas.Top);
   
        /// <summary>
        /// Горизонтальная атака
        /// </summary>
        #region
        public int countHorizontAttack = 0; // Кол-во горизонтальных атак;
        private UIElement[] dragonAttackObj = new UIElement[2]; //Объекты горизонтальной атаки;
        private DispatcherTimer timerCheckHitHoriz; // Таймер проверки на попадание горизонтальной атакой по герою;
        private del showLich; // Появление луча горизонтальной атаки;
        private del unShowLich; // Отключение луча горизонтальной атаки;
        #endregion

        private delegate void del();
        public Random rand = new Random(); 
        
        public TypeAttack(MainWindow main,double wallsTop)
        {
            gameApp = main;

            hero = main.player;
            boss = main.bossEnemy;
            coorWallsTop = wallsTop;
            mapGame = main.gamePanel;

            SetLichDel();
        }    

        /// <summary>
        /// СУПЕЕЕЕР АТАКА БОМБ 
        /// </summary>
        /// <param name="timer">Таймер с появлением бомб</param>
        /// <returns>Таймер</returns>
        public DispatcherTimer SuperSpawnMins(DispatcherTimer timer)
        {
            timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.10) };
            int countMin = 0;
            timer.Tick += (s, es) =>
            {              
                mapGame.Children.Add(SpawnMins(boss));
                countMin++;

                if (countMin >= 25)
                {
                    timer.Stop();
                    countHorizontAttack = 0;
                    statusMoveAttackBoss = true;
                    timer = StartSpawnMins(timer);
                }
            };

            timer.Start();

            return timer;
        }

        /// <summary>
        /// Создание таймера с появлением бомб
        /// </summary>
        /// <param name="timer">Таймер появления бомб</param>
        /// <returns>Таймер</returns>
        public DispatcherTimer StartSpawnMins(DispatcherTimer timer)
        {
            timer = new DispatcherTimer { Interval = speedSpawnDeffBomb };
            timer.Tick += (s, es) =>
            {
                mapGame.Children.Add(SpawnMins(boss));

                if (countHorizontAttack >= 2)
                {
                    timer.Stop();
                    if (statusSuperSpawnBomb)
                        timer = SuperSpawnMins(timer);
                    else
                    {
                        countHorizontAttack = 0;
                        statusMoveAttackBoss = true;
                    }
                }
            };

            return timer;
        }

        /// <summary>
        /// Запуск процесса горизонтальной атаки с рандомной стороны
        /// </summary>
        /// <returns>Враг</returns>
        public Image HorizontAttack()
        {

            int scaleDragon = 1;
            double top = coorWallsTop - gameApp.dragonAttacks.Height;
            double moveDragon = 230;

            //Рандомим сторону с которой появится враг
            if (rand.Next(0, 2) == 0)
                scaleDragon = -1;
            
            //Рандомим высоту с которой появится враг
            if (rand.Next(0, 2) == 0)
            {
                moveDragon = top;
                top = 230;
            }

            Image dragon = CreateEnemyDragon();

            if (scaleDragon == 1)
                dragon.Margin = new Thickness(-100, (top + 50), 0, 0);
            else
                dragon.Margin = new Thickness((650) - dragon.Width, (top + 50), 0, 0);


            dragon.RenderTransform = new ScaleTransform(scaleDragon, 1);

            DragonMove(dragon,false, scaleDragon, top, moveDragon);          
            
            return dragon;
        }

        /// <summary>
        /// Создание шипов у пола
        /// </summary>
        /// <returns>Шипы</returns>
        public Rectangle CreateSpikes()
        {
            Rectangle spikes = new Rectangle
            {
                Fill = new ImageBrush(new BitmapImage(new Uri("Images/Ship.png", UriKind.RelativeOrAbsolute))),

                Width = gameApp.Wall.Width,
                Height = gameApp.Wall.Height,
            };

            Canvas.SetLeft(spikes,spikes.Width * rand.Next(0,3));
            Canvas.SetTop(spikes,Canvas.GetTop(gameApp.Walls) + spikes.Height);

            MoveSpikes(spikes, Canvas.GetTop(gameApp.Walls),TimeSpan.FromSeconds(1.5), TimeSpan.FromMilliseconds(400),false);
            return spikes;
        }

        /// <summary>
        /// Вторая атака босса (сверху-вниз)
        /// </summary>
        /// <param name="scaleX">Размер босса (Scale.X)</param>
        /// <param name="timerCheckHitBossAtHero">Таймер с проверкой на попадание босса по герою</param>
        /// <param name="topProperty">Состояние перемещения босса (идентификатор куда босс полетит (вверх или влево))</param>
        /// <param name="to">Координаты куда босс будет стремится дойти</param>
        /// <param name="timeSpeed">Время за которое он туда дойдет</param>
        /// <param name="cancelAttackBoss">Отмена атаки босса</param>
        public void MoveBossAttack(int scaleX,DispatcherTimer timerCheckHitBossAtHero ,bool topProperty, double to, TimeSpan timeSpeed, bool cancelAttackBoss)
        {
            DoubleAnimation moveBoss = new DoubleAnimation
            {
                Duration = timeSpeed,

                To = to
            };

            if (timerCheckHitBossAtHero == null)
            {
                timerCheckHitBossAtHero = CheckHitBossAtHero();
                timerCheckHitBossAtHero.Start();
            }

            moveBoss.Completed += async (s, e) =>
                {
                    await Task.Delay(delayAnimationBoss);

                    if(topProperty && !cancelAttackBoss)
                    {
                        double toMove = (scaleX == 1) ? gameApp.ActualWidth - boss.ActualWidth : 0 ;
                        boss.RenderTransform = new ScaleTransform(-scaleX, 1);        
                        MoveBossAttack(-scaleX,timerCheckHitBossAtHero, false, toMove, speedAnimationBossLR,false);
                    }
                    
                    else
                    {

                        if (!cancelAttackBoss)
                        {
                            MoveBossAttack(scaleX, timerCheckHitBossAtHero, true, 0, speedAnimationBossTB, true);
                            boss.RenderTransform = new ScaleTransform(-scaleX, 1);
                        }
                        else
                        {
                            double toMove = ((Canvas.GetLeft(boss) == 0) ? gameApp.ActualWidth - boss.ActualWidth : 0);
                            gameApp.StartMoveBoss(toMove, scaleX);
                            statusMoveAttackBoss = false;
                            timerCheckHitBossAtHero.Stop();
                            gameApp.StartDefautAttackBoss();
                        }
                    }
                };
                boss.BeginAnimation((topProperty) ? Canvas.TopProperty : Canvas.LeftProperty, moveBoss);
        }


        //Босс (Вторая атака)
        #region

        /// <summary>
        /// Проверка на попадание босса по герою
        /// </summary>
       /// <returns>Таймер с проверкой</returns>
        private DispatcherTimer CheckHitBossAtHero()
        {

            DispatcherTimer timerCheckHit = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };

            bool trDamageBossAtHero = false;
            bool trDamageHeroAtBoss = false; 

            timerCheckHit.Tick += (s, e) =>
            {
                Rect her;
                Rect bosses;

                if (hero.RenderTransform.Value.ToString().Equals("Identity"))
                    her = new Rect(Canvas.GetLeft(hero), Canvas.GetTop(hero), hero.ActualWidth, hero.ActualHeight);
                else
                    her = new Rect((Canvas.GetLeft(hero)) - hero.ActualWidth, Canvas.GetTop(hero), hero.ActualWidth, hero.ActualHeight);

                if (boss.RenderTransform.Value.ToString().Equals("Identity"))              
                    bosses = new Rect(Canvas.GetLeft(boss) + 50, Canvas.GetTop(boss) + 70, boss.Width - 70, boss.Height - 70);                
                else               
                    bosses = new Rect(Canvas.GetLeft(boss) + 10, Canvas.GetTop(boss) + 70, boss.Width - 60, boss.Height - 70);

                   

                if (bosses.IntersectsWith(her) && !trDamageBossAtHero)
                {
                    DamageOnHero();
                    trDamageBossAtHero = true;
                }
              
                 Rect bossesDamage;
                 Rect herAttack;

                 if (hero.RenderTransform.Value.ToString().Equals("Identity"))
                     herAttack = new Rect(Canvas.GetLeft(hero), Canvas.GetTop(hero) + hero.ActualHeight - 5, hero.ActualWidth,5);
                 else
                     herAttack = new Rect((Canvas.GetLeft(hero)) - hero.ActualWidth, Canvas.GetTop(hero) + hero.ActualHeight - 5, hero.ActualWidth, 5);

                 if (boss.RenderTransform.Value.ToString().Equals("Identity"))
                 {
                     bossesDamage = new Rect(Canvas.GetLeft(boss) + 50, Canvas.GetTop(boss) + 40, boss.Width - 70, 30);
                 }
                 else
                 {
                     bossesDamage = new Rect(Canvas.GetLeft(boss) + 20, Canvas.GetTop(boss) + 40, boss.Width - 70, 30);
                 }
                 if (bossesDamage.IntersectsWith(herAttack) && !trDamageHeroAtBoss)
                {
                    DamageOnBoss(--hpBoss);
                    trDamageHeroAtBoss = true;
                    gameApp.JumpHero(120,TimeSpan.FromSeconds(0.3));
                }

            };

            return timerCheckHit;
        }

        #endregion

        //Шипы//
        #region

        /// <summary>
        /// Анимация перемещение шипов
        /// </summary>
        /// <param name="spikes">Шипы</param>
        /// <param name="top">Высота куда шипы должны дойти</param>
        /// <param name="timeSpeed">Время за которое шиб дойдет до цели</param>
        /// <param name="tuskTime">Задержка перед переключение новой анимации</param>
        /// <param name="deleteObj">Состояние удаление объекта</param>
        private void MoveSpikes(Rectangle spikes, double top, TimeSpan timeSpeed, TimeSpan tuskTime, bool deleteObj)
        {
            DoubleAnimation moveTop = new DoubleAnimation
            {
                Duration = timeSpeed,
                To = top
            };

            moveTop.Completed += async (s, e) =>
                {
                    await Task.Delay(tuskTime);

                    if (deleteObj)
                        mapGame.Children.Remove(spikes);
                    else
                    {
                        if (tuskTime == TimeSpan.FromMilliseconds(400))
                        {
                            MoveSpikes(spikes, Canvas.GetTop(spikes) - spikes.ActualHeight, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(50), false);
                            CheckHitHeroSpike(spikes);
                        }
                        else
                            MoveSpikes(spikes, Canvas.GetTop(gameApp.Walls) + spikes.Height, TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(0), true);
                    }
                };

            spikes.BeginAnimation(Canvas.TopProperty, moveTop);
        }

        /// <summary>
        /// Проверка на попадание шипов по герою
        /// </summary>
        /// <param name="spikes">Шипы</param>
        private void CheckHitHeroSpike(Rectangle spikes)
        {
            DispatcherTimer timerCheckHit = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };

            timerCheckHit.Tick += (s, e) =>
                {
                    Rect her;
                    if (hero.RenderTransform.Value.ToString().Equals("Identity"))
                        her = new Rect(Canvas.GetLeft(hero), Canvas.GetTop(hero), hero.ActualWidth, hero.ActualHeight);
                    else
                        her = new Rect((Canvas.GetLeft(hero)) - hero.ActualWidth, Canvas.GetTop(hero), hero.ActualWidth, hero.ActualHeight);

                    Rect spike = new Rect(Canvas.GetLeft(spikes), Canvas.GetTop(spikes), spikes.Width, spikes.Height);

                    if (spike.IntersectsWith(her))
                    {
                        DamageOnHero();
                        (s as DispatcherTimer).Stop();
                    }
                    if (!mapGame.Children.Contains(spikes))                  
                        (s as DispatcherTimer).Stop();
                    
                };
            timerCheckHit.Start();
        }

        #endregion

        //Горизонтальная атака//
        #region

        /// <summary>
        /// Устанавливаем делегаты для отображения луча 
        /// </summary>
        private void SetLichDel()
        {
            showLich = async () =>
            {
                await Task.Delay(1100);
                dragonAttackObj[1].Visibility = Visibility.Visible;
                unShowLich();
            };

            unShowLich = async () =>
            {
                await Task.Delay(1500);
                dragonAttackObj[1].Visibility = Visibility.Hidden;
            };
        }
        
        /// <summary>
        /// Отображение врага
        /// </summary>
        /// <returns>Враг</returns>
        private Image CreateEnemyDragon()
        {
           return new Image
            {
                Source = new BitmapImage(new Uri("Images/HorizAttack.gif", UriKind.RelativeOrAbsolute)),
                Stretch = Stretch.None,
                Width = gameApp.dragon.Width,
                Height = gameApp.dragon.Height,
                RenderTransformOrigin = new Point(0.5, 0.5),

                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };
        }

        /// <summary>
        /// Перемещение врага к точке его атаки
        /// </summary>
        /// <param name="dragon">Враг</param>
        /// <param name="leaveDragon">Уход врага из формы</param>
        /// <param name="scale">Размер</param>
        /// <param name="top">Отступ</param>
        /// <param name="moveDragon">Конечная точка перемещения врага</param>
        private void DragonMove(Image dragon , bool leaveDragon, int scale, double top, double moveDragon)
        {
            ThicknessAnimation moveDrag = new ThicknessAnimation
            {
                Duration = TimeSpan.FromSeconds(1.25)                   
            };

            if (scale == 1)
            {
                if(!leaveDragon)
                    moveDrag.To = new Thickness(0, top, 0, 0);
                else
                    moveDrag.To = new Thickness(-100, top - 75, 0, 0);
            }
            else
            {
                if (!leaveDragon)
                    moveDrag.To = new Thickness(650 - dragon.Width * 2.85, top, 0, 0);
                else
                    moveDrag.To = new Thickness(650, top - 75, 0, 0);
            }
            moveDrag.Completed += async (s, e) =>
                {
                    await Task.Delay(450);
                    mapGame.Children.Remove(dragon);
                    if(!leaveDragon)
                        mapGame.Children.Add(DragonBallAttack(scale, top, moveDragon));
                };

            dragon.BeginAnimation(Canvas.MarginProperty, moveDrag);           
        }

        /// <summary>
        ///  Создает горизонтальную атаку 
        /// </summary>
        /// <param name="scale">Размер</param>
        /// <param name="top">Отступ</param>
        /// <param name="moveDragon">Конечная точка перемещения врага</param>
        /// <returns>Враг</returns>
        private StackPanel DragonBallAttack(int scale, double top , double moveDragon)
        {

            StackPanel dragonAttack = new StackPanel
            {
                Width = gameApp.dragonAttacks.Width,
                Height = gameApp.dragonAttacks.Height,

                RenderTransformOrigin = new Point(0.5,0.5),
                Orientation = Orientation.Horizontal
            };

            dragonAttackObj[0] = new Image 
            {
                Width = gameApp.dragon.Width,
                Height = gameApp.dragon.Height,
                Stretch = System.Windows.Media.Stretch.None,
                Margin = gameApp.dragon.Margin
            };

             var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri("Images/HorizAttack.gif",UriKind.RelativeOrAbsolute);
            image.EndInit();

            ImageBehavior.SetAnimatedSource((dragonAttackObj[0] as Image), image);
            Canvas.SetLeft(dragonAttack, 0);

            dragonAttackObj[1] = new Rectangle
            {
                Width = gameApp.lich.Width - 20,               
                Fill = gameApp.lich.Fill,
                Margin = gameApp.lich.Margin,

                Visibility = Visibility.Hidden
            };

            showLich();

            dragonAttack.Children.Add(dragonAttackObj[0]);
            dragonAttack.Children.Add(dragonAttackObj[1]);

            dragonAttack.RenderTransform = new ScaleTransform(scale, 1);
            Canvas.SetTop(dragonAttack, top);

            dragonAttack.BeginAnimation(Canvas.TopProperty, SetAnimationDragon(dragonAttack, moveDragon));

            timerCheckHitHoriz = CheckToHitHorizontAttack(dragonAttack);
            return dragonAttack;
        }
        
        /// <summary>
        /// Анимация горизонтальной атаки
        /// </summary>
        /// <param name="dragonAttack">Объект с анимацией</param>
        /// <param name="to">Конечные координаты анимации</param>
        /// <returns>Анимация</returns>
        private DoubleAnimation SetAnimationDragon(StackPanel dragonAttack, double to)
        {
            DoubleAnimation anime = new DoubleAnimation 
            { 
                Duration = TimeSpan.FromSeconds(3.3),
                From = Canvas.GetTop(dragonAttack),
                To = to
            };

            anime.Completed += (s, e) =>
                {
                    CompleteAttackDragon(dragonAttack);
                };
            return anime;
        }

        /// <summary>
        /// Проверка на попадание луча по герою
        /// </summary>
        private DispatcherTimer CheckToHitHorizontAttack(StackPanel dragonAttack)
        {
            DispatcherTimer timerCheckHit = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };

            timerCheckHit.Tick += (s, e) =>
                {
                    if ((dragonAttackObj[1] as Rectangle).Visibility == Visibility.Visible && statusHorizontAttack)
                    {
                        Rect her;

                        Rect lich;
                        if (hero.RenderTransform.Value.ToString().Equals("Identity"))
                            her = new Rect(Canvas.GetLeft(hero), Canvas.GetTop(hero), hero.ActualWidth, hero.ActualHeight);
                        else
                            her = new Rect((Canvas.GetLeft(hero)) - hero.ActualWidth, Canvas.GetTop(hero), hero.ActualWidth, hero.ActualHeight);

                        if (!dragonAttack.RenderTransform.Value.ToString().Equals("Identity"))
                            lich = new Rect(0, Canvas.GetTop(dragonAttack), (dragonAttackObj[1] as Rectangle).Width, (dragonAttackObj[1] as Rectangle).ActualHeight);
                        else
                            lich = new Rect((dragonAttackObj[0] as Image).Width, Canvas.GetTop(dragonAttack), (dragonAttackObj[1] as Rectangle).Width, (dragonAttackObj[1] as Rectangle).ActualHeight);

                        //  MessageBox.Show((dragonAttackObj[1] as Rectangle).Width.ToString() + " " + (dragonAttackObj[1] as Rectangle).ActualHeight.ToString());

                        if (lich.IntersectsWith(her))
                        {
                            DamageOnHero();
                            (s as DispatcherTimer).Stop();
                        }
                    }
                };

            timerCheckHit.Start();

            return timerCheckHit;
        }  

        /// <summary>
        /// Заверщение горизонтальной атаки
        /// </summary>
        /// <param name="dragonAttack">Объект с анимацией</param>
        private void CompleteAttackDragon(StackPanel dragonAttack)
        {
            Image dragon = CreateEnemyDragon();

            mapGame.Children.Add(dragon);

            int scale = 1;
            if (!dragonAttack.RenderTransform.Value.ToString().Equals("Identity"))
            {
                scale = -1;
                dragon.RenderTransform = new ScaleTransform(-1, 1);
                dragon.Margin = new Thickness(Canvas.GetLeft(dragonAttack) + dragonAttack.ActualWidth - (dragon.Width * 2.85), Canvas.GetTop(dragonAttack), 0, 0);
            }
            else
                dragon.Margin = new Thickness(Canvas.GetLeft(dragonAttack), Canvas.GetTop(dragonAttack), 0, 0);

            DragonMove(dragon, true, scale, dragon.Margin.Top, 0);
            mapGame.Children.Remove(dragonAttack);

            timerCheckHitHoriz.Stop();
            countHorizontAttack++;
        }

        #endregion

        //Бомбы// 
        #region

        /// <summary>
        /// Создает мины выпускаемым боссам
        /// </summary>
        /// <param name="boss">Босс на форме</param>
        /// <returns>Мину</returns>
        private Ellipse SpawnMins(Image boss)
        {
            Ellipse mina = new Ellipse
            {
                Fill = Brushes.Black,
                Width = 30,
                Height = 30,
            };
                     
            Canvas.SetTop(mina, Canvas.GetTop(boss) + boss.Height / 2);

            if (boss.RenderTransform.Value.ToString().Equals("Identity"))
                Canvas.SetLeft(mina, Canvas.GetLeft(boss) + boss.ActualWidth - 30);
            else
                Canvas.SetLeft(mina, Canvas.GetLeft(boss));
            StartFallMin(mina);
            
            return mina;
        }

        /// <summary>
        /// Запуск падения мины
        /// </summary>
        /// <param name="mina">Мина</param>
        private void StartFallMin(Ellipse mina)
        {
            DispatcherTimer fallTimer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromMilliseconds(3),             
            };

            fallTimer.Tick += (s, e) =>
                {
                    if (Canvas.GetTop(mina) + mina.Height < coorWallsTop)
                        Canvas.SetTop(mina, Canvas.GetTop(mina) + 5);
                    else
                    {
                        mapGame.Children.Remove(mina);           
                        mapGame.Children.Add(CreateBOOM(mina));
                        (s as DispatcherTimer).Stop();
                    }

                    //Если мина попала в героя
                    if(CheckHitMinaOnHero(mina))
                    {                      
                        mapGame.Children.Remove(mina);
                        mapGame.Children.Add(CreateBOOM(mina));
                        (s as DispatcherTimer).Stop();

                        if (gameApp.panelHpHero.Children.Count > 0)
                            gameApp.panelHpHero.Children.RemoveAt(gameApp.panelHpHero.Children.Count - 1);

                        gameApp.CheckDieHero(--hpHero);
                    }
                };

            fallTimer.Start();
        }

        /// <summary>
        /// Проверка на попадание мины в героя
        /// </summary>
        /// <param name="mina">Мина</param>
        /// <returns>Состояние попадания мины в героя</returns>
        private bool CheckHitMinaOnHero(Ellipse mina)
        {
            Rect her;
            if (hero.RenderTransform.Value.ToString().Equals("Identity"))
                her = new Rect(Canvas.GetLeft(hero), Canvas.GetTop(hero) + 5, hero.ActualWidth, hero.ActualHeight - 5);
            else
                her = new Rect((Canvas.GetLeft(hero)) - hero.ActualWidth, Canvas.GetTop(hero), hero.ActualWidth, hero.ActualHeight - 5);

            Rect min = new Rect(Canvas.GetLeft(mina), Canvas.GetTop(mina) + 5, mina.ActualWidth, mina.ActualHeight - 5);

            if (min.IntersectsWith(her))
                return true;

            return false;
        }

        /// <summary>
        /// Создание анимации взрыва
        /// </summary>
        /// <param name="mina">Мина</param>
        /// <returns>Врыв</returns>
        private Image CreateBOOM(Ellipse mina)
        {

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri("Images/Boom.gif",UriKind.RelativeOrAbsolute);
            image.EndInit();

            Image boomGif = new Image
            {
                Width = 100,
                Height = 100
            };

            Canvas.SetTop(boomGif,Canvas.GetTop(mina) - (boomGif.Height - mina.Height) + 10);
            Canvas.SetLeft(boomGif,Canvas.GetLeft(mina) - mina.Width / 2);

            ImageBehavior.SetAnimatedSource(boomGif, image);

            DeleteObj(boomGif);
            return boomGif;
        }

        /// <summary>
        /// Удаление анимации взрыва
        /// </summary>
        /// <param name="boomGif">Взрыв</param>
        private async void DeleteObj(Image boomGif)
        {
            await Task.Delay(1150);
            mapGame.Children.Remove(boomGif);
        }

        #endregion


        /// <summary>
        /// Устанавливаем сложность игры
        /// </summary>
        /// <param name="hpBoss">Здоровье босса</param>
        private void SetСomplexityGame(int hpBoss)
        {
            switch(hpBoss)
            {
                case 9:                                     
                    statusHorizontAttack = true;
                    break;
                case 7:
                    gameApp.StartSpawnSpike();
                    statusSuperSpawnBomb = true;
                    break;

                case 5:
                    speedSpawnDeffBomb = TimeSpan.FromSeconds(1);
                    speedAnimationBossLR = TimeSpan.FromSeconds(0.9);
                    speedAnimationBossTB = TimeSpan.FromSeconds(1.5);
                    delayAnimationBoss = TimeSpan.FromMilliseconds(200);
                    break;

                case 3:
                    speedSpawnDeffBomb = TimeSpan.FromSeconds(0.85);
                    speedAnimationBossLR = TimeSpan.FromSeconds(0.8);
                    speedAnimationBossTB = TimeSpan.FromSeconds(1.25);
                    delayAnimationBoss = TimeSpan.FromMilliseconds(100);
                    break;

                case 2:
                    speedSpawnDeffBomb = TimeSpan.FromSeconds(0.70);
                    speedAnimationBossLR = TimeSpan.FromSeconds(0.75);
                    speedAnimationBossTB = TimeSpan.FromSeconds(1.15);
                    delayAnimationBoss = TimeSpan.FromMilliseconds(50);
                    break;

                case 1:
                    speedSpawnDeffBomb = TimeSpan.FromSeconds(0.50);
                    speedAnimationBossLR = TimeSpan.FromSeconds(0.65);
                    speedAnimationBossTB = TimeSpan.FromSeconds(0.85);
                    delayAnimationBoss = TimeSpan.FromMilliseconds(0);
                    break;
            }
        }

        /// <summary>
        /// Нанесение урона герою
        /// </summary>
        private void DamageOnHero()
        {
            if (gameApp.panelHpHero.Children.Count > 0)
                gameApp.panelHpHero.Children.RemoveAt(gameApp.panelHpHero.Children.Count - 1);

            gameApp.CheckDieHero(--hpHero);
        }

        /// <summary>
        /// Нанесение урона по боссу
        /// </summary>
        /// <param name="hpBoss">Хп босса</param>
        private void DamageOnBoss(int hpBoss)
        {
            StackPanel panelHpBoss = gameApp.panelHpBosses.Children.OfType<StackPanel>().ToList()[0];

            if (panelHpBoss.Children.Count > 0)
                panelHpBoss.Children.RemoveAt(panelHpBoss.Children.Count - 1);

            if (hpBoss == 0)
            {
                mapGame.Children.Remove(boss);
                boss.BeginAnimation(Canvas.LeftProperty, null);
                boss.BeginAnimation(Canvas.TopProperty, null);
                gameApp.StopThisGame();
                MessageBox.Show("Ты победил!!!");
                gameApp.Close();
            }
            else
                SetСomplexityGame(hpBoss);
        }
    }
}
