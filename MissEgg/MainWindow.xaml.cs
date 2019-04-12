using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MissEgg
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private int typeAnimationHero = 1; // Анимация героя;
        private int scaleHero = 1; // scale героя;

        private const double speedPlayer = 15; // Скорость игрока
        private bool[] flyHero = { false, false}; // Состояние прыжков героя;
        private bool fallHero = false; // Состояние падения героя;

        private TypeAttack attack; // Логика враждебной среды;
        private DispatcherTimer[] timerMoveLR = new DispatcherTimer[2]; // Тамера отвечающие за плавное перемещение;
        public DispatcherTimer timerSpawnSpikes; // Таймер появления шипов;
        private DispatcherTimer timerSpawnBomb; // Таймер появления бомб; 
        private DispatcherTimer timerSpawnDragon; // Таймер появления горизонтальной атаки; 

        /// <summary>
        /// Загрузка окна
        /// </summary>
        /// <param name="sender">Window</param>
        /// <param name="e">Loaded</param>
        private void StartGame(object sender, RoutedEventArgs e)
        {
            StartMoveBoss(this.ActualWidth - bossEnemy.Width,1);
            timerMoveLR[0] = new DispatcherTimer(DispatcherPriority.Send);
            timerMoveLR[1] = new DispatcherTimer(DispatcherPriority.Send);

            attack = new TypeAttack(this,Canvas.GetTop(Walls));

            //Создание враждебной среды
            StartDefautAttackBoss();

            for (int i = 0; i < attack.hpHero; i++)
            {
                Rectangle rec = new Rectangle
                {
                    Fill = Brushes.Red,
                    Margin = new Thickness(5, 0, 0, 0),

                    Width = 15,
                    Height = 15,
                };
                panelHpHero.Children.Add(rec);
            }

            StackPanel panelHpBoss = new StackPanel { Orientation = Orientation.Horizontal };
            for (int i = 0; i < attack.hpBoss; i++)
            {
                Rectangle rec = new Rectangle
                {
                    Fill = Brushes.DarkRed,
                    Margin = new Thickness(5, 0, 0, 0),

                    Width = 15,
                    Height = 15,
                };
                panelHpBoss.Children.Add(rec);
            }

            Canvas.SetLeft(panelHpBosses, this.Width / 2 - panelHpBosses.ActualWidth - 15);
            panelHpBosses.Children.Add(panelHpBoss);

        }

        /// <summary>
        /// Запуск обычных атак босса
        /// </summary>
        public void StartDefautAttackBoss()
        {
            timerSpawnBomb = attack.StartSpawnMins(timerSpawnBomb);
            timerSpawnBomb.Start();
            timerSpawnDragon = new DispatcherTimer { Interval = TimeSpan.FromSeconds(attack.rand.Next(7,11)) };

            timerSpawnDragon.Tick += (s, es) =>
            {
                if(!attack.statusMoveAttackBoss && attack.statusHorizontAttack)
                    gamePanel.Children.Add(attack.HorizontAttack());
                else if (!attack.statusHorizontAttack)             
                    attack.countHorizontAttack++;              

                (s as DispatcherTimer).Interval = TimeSpan.FromSeconds(attack.rand.Next(14, 22));
            };
            timerSpawnDragon.Start();
        }

        /// <summary>
        /// Запуск появления шипов
        /// </summary>
        public void StartSpawnSpike()
        {
            timerSpawnSpikes = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timerSpawnSpikes.Tick += (s, es) =>
            {
                (s as DispatcherTimer).Interval = TimeSpan.FromSeconds(attack.rand.Next(5, 15));
                gamePanel.Children.Add(attack.CreateSpikes());
            };

            timerSpawnSpikes.Start();
        }

        /// <summary>
        /// Запуск перемещения босса по форме
        /// </summary>
        /// <param name="to">Конечные координаты босса</param>
        /// <param name="scaleBoss">Размер босса (Scale.X)</param>
        public void StartMoveBoss(double to , int scaleBoss)
        {
            DoubleAnimation moveBoss = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(3.25),
                From = Canvas.GetLeft(bossEnemy),
                To = to
            };

            moveBoss.Completed += (s, e) =>
            {
                if (!attack.statusMoveAttackBoss)
                {
                    StartMoveBoss(moveBoss.From.Value, -scaleBoss);
                    bossEnemy.RenderTransform = new ScaleTransform(scaleBoss, 1);
                }
                else
                {
                    
                    bossEnemy.RenderTransform = new ScaleTransform(scaleBoss, 1);
                    double toMoveBoss =  attack.coorWallsTop - bossEnemy.ActualHeight;

                    timerSpawnDragon.Stop();
                    timerSpawnBomb.Stop();

                    attack.MoveBossAttack(-scaleBoss, null, true, toMoveBoss, attack.speedAnimationBossTB, false);
                }
            };

            bossEnemy.BeginAnimation(Canvas.LeftProperty, moveBoss);

        }


        /// <summary>
        /// Нажатие на клавишу
        /// </summary>
        /// <param name="sender">Window</param>
        /// <param name="e">KeyDown</param>
        private void PressKey(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.A:              
                    MoveHeroLeft();
                break;

                case Key.D:                             
                    MoveHeroRight();
                break;

                case Key.Space:
                    if (!flyHero[1])
                    {
                        if (flyHero[0])
                        {
                            flyHero[1] = true;
                            JumpHero(145,TimeSpan.FromSeconds(0.4));
                        }
                        else
                            JumpHero(100,TimeSpan.FromSeconds(0.25));
                    }
                    break;
            }
        }

        /// <summary>
        /// Отмена нажатия клавиши 
        /// </summary>
        /// <param name="sender">Window</param>
        /// <param name="e">KeyUp</param>
        private void UnPressKey(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.A:
                    timerMoveLR[0].Stop();
                    break;

                case Key.D:
                    timerMoveLR[1].Stop();
                    break;
            }
        }

        /// <summary>
        /// Перемещение героя влево
        /// </summary>
        private void MoveHeroLeft()
        {
            if (timerMoveLR[1].IsEnabled)
                timerMoveLR[1].Stop();

            if (!timerMoveLR[0].IsEnabled)
            {
                timerMoveLR[0] = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(speedPlayer) };
                timerMoveLR[0].Tick += (s, e) =>
                    {
                        if (Canvas.GetLeft(player) - 5 >= 0)
                            Canvas.SetLeft(player, Canvas.GetLeft(player) - 5);

                        if (Canvas.GetLeft(player) + 15 > this.ActualWidth)
                            Canvas.SetLeft(player, this.ActualWidth);
                    };
                timerMoveLR[0].Start();
            }

            if (typeAnimationHero >= 3)         
                typeAnimationHero = 0;

            if (scaleHero == -1)
            {
                scaleHero = 1;
                player.RenderTransform = new ScaleTransform(scaleHero, 1);
                Canvas.SetLeft(player, Canvas.GetLeft(player) - player.Width);
            }

            typeAnimationHero++;
            player.Fill = new ImageBrush(new BitmapImage(new Uri("Images/Anim" + typeAnimationHero + ".png", UriKind.RelativeOrAbsolute)));
        }

        /// <summary>
        /// Перемещение героя вправо
        /// </summary>
        private void MoveHeroRight()
        {
            if (timerMoveLR[0].IsEnabled)
                timerMoveLR[0].Stop();

            if (!timerMoveLR[1].IsEnabled)
            {
                timerMoveLR[1] = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(speedPlayer) };
                timerMoveLR[1].Tick += (s, e) =>
                {
                    if (Canvas.GetLeft(player) < this.ActualWidth)
                        Canvas.SetLeft(player, Canvas.GetLeft(player) + 5);
                    else
                        Canvas.SetLeft(player, this.ActualWidth);
                };
                timerMoveLR[1].Start();
            }

            if (typeAnimationHero >= 3)
                typeAnimationHero = 0;

            if (scaleHero == 1)
            {
                scaleHero = -1;
                player.RenderTransform = new ScaleTransform(scaleHero, 1);
                Canvas.SetLeft(player, Canvas.GetLeft(player) + player.Width);
            }

            typeAnimationHero++;
            player.Fill = new ImageBrush(new BitmapImage(new Uri("Images/Anim" + typeAnimationHero + ".png", UriKind.Relative)));
        }

        /// <summary>
        /// Прыжок героя
        /// </summary>
        /// <param name="heightJump">Высота прыжка героя</param>
        /// <param name="speedJump">Время за которое игрок дойдет до нужной цели</param>
        public void JumpHero(int heightJump, TimeSpan speedJump)
        {
            fallHero = false;

            flyHero[0] = true;
            DoubleAnimation jump = new DoubleAnimation
            {
                Duration = speedJump,
                From = Canvas.GetTop(player),
                To = Canvas.GetTop(player) - heightJump
            };
            
            jump.Completed += (s, e) =>
            {
                HeroFall();           
            };

            player.BeginAnimation(Canvas.TopProperty, jump);
        }

        /// <summary>
        /// Падение героя
        /// </summary>
        private void HeroFall()
        {
            fallHero = true;
            flyHero[0] = true;
          
            DoubleAnimation jump = new DoubleAnimation
            {
                Duration = TimeSpan.FromSeconds(0.28),
                From = Canvas.GetTop(player),
                To = Canvas.GetTop(Walls) - player.Height
            };

            jump.Completed += (s, e) =>
            {
                if (fallHero)
                {
                    flyHero[0] = false;
                    flyHero[1] = false;
                }
            };

            player.BeginAnimation(Canvas.TopProperty, jump);
        }

        /// <summary>
        /// Останавливает все таймера в игре
        /// </summary>
        public void StopThisGame()
        {
            timerSpawnBomb.Stop();
            timerSpawnDragon.Stop();
            timerSpawnSpikes.Stop();
        }

        /// <summary>
        /// Проверка на смерть
        /// </summary>
        /// <param name="hpHero">Здоровье героя</param>
        public void CheckDieHero(int hpHero)
        {
            if (hpHero == 0 && attack.hpBoss > 0)
            {
                player.Visibility = Visibility.Hidden;
                MessageBox.Show("Ты умер(", "#_#");
                Close();
            }
        }
     
    }
}
