using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Adaptive.ReactiveTrader.Client.Android.UI.Blotter;
using Adaptive.ReactiveTrader.Client.Android.UI.Prices;
using Adaptive.ReactiveTrader.Client.Android.UI.Status;
using Adaptive.ReactiveTrader.Client.Concurrency;
using Adaptive.ReactiveTrader.Client.Domain;
using Adaptive.ReactiveTrader.Client.Domain.Transport;
using Adaptive.ReactiveTrader.Client.UI.Shell;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics.Drawables;
using Android.Media;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Autofac;
using Java.Lang;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;
using String = Java.Lang.String;

namespace Adaptive.ReactiveTrader.Client.Android
{
    [Activity(MainLauncher = true, Icon = "@drawable/icon", Label = "Reactive Trader",
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public class MainActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            if (App.IsTablet)
            {
                InitTablet();
            }
            else
            {
                InitPhone();
            }
        }

        private void InitTablet()
        {
            SetContentView(Resource.Layout.TabletContainer);

            var container = FindViewById<RelativeLayout>(Resource.Id.fragmentContainer);
            var blotterViewId = View.GenerateViewId();
            var pricesView = PricesListFragment.OnCreateView(LayoutInflater, container, null);
            var pricesParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            pricesParams.AddRule(LayoutRules.Above, blotterViewId);
            pricesView.LayoutParameters = pricesParams;
            pricesView.Id = View.GenerateViewId();

            container.AddView(pricesView);

            var blotterView = BlotterFragment.OnCreateView(LayoutInflater, container, null);
            var layoutParams = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, 280);
            blotterView.Id = blotterViewId;
            layoutParams.AddRule(LayoutRules.AlignParentBottom);
            blotterView.LayoutParameters = layoutParams;

            container.AddView(blotterView);
        }

        private void InitPhone()
        {
            SetContentView(Resource.Layout.PhoneContainer);
            var adapter = new TabsAdapter(SupportFragmentManager);
            var viewPager = FindViewById<ViewPager>(Resource.Id.viewPager);
            viewPager.Adapter = adapter;
            var tabLayout = FindViewById<TabLayout>(Resource.Id.tabLayout);
            tabLayout.SetupWithViewPager(viewPager);

            var reactiveTrader = App.Container.Resolve<IReactiveTrader>();
            reactiveTrader.ConnectionStatusStream
                .Where(ci => ci.ConnectionStatus == ConnectionStatus.Connected)
                .Timeout(TimeSpan.FromSeconds(15))
                .ObserveOn(App.Container.Resolve<IConcurrencyService>().Dispatcher)
                .Subscribe(
                    _ =>
                    {
                        var spinner = FindViewById<ProgressBar>(Resource.Id.ProgressBar);
                        spinner.Visibility = ViewStates.Invisible;
                        var logo = FindViewById<FrameLayout>(Resource.Id.logo);
                        logo.Animate()
                            .SetDuration(1000)
                            .Alpha(0)
                            .WithLayer()
                            .WithEndAction(new Runnable(() => logo.Visibility = ViewStates.Gone))
                            .Start();
                        
                    },
                    ex => Console.WriteLine("Failed")
                );
        }

        static BlotterFragment BlotterFragment => new BlotterFragment(App.Container.Resolve<IShellViewModel>());
        static PricesListFragment PricesListFragment => new PricesListFragment(
            App.Container.Resolve<IShellViewModel>(),
            App.Container.Resolve<IConcurrencyService>());

        static StatusFragment StatusFragment
            => new StatusFragment(App.Container.Resolve<IReactiveTrader>(), App.Container.Resolve<IConcurrencyService>());

        class TabsAdapter : FragmentPagerAdapter
        {
            readonly string[] _tabName =
            {
                "Prices",
                "Trades",
                "Status"
            };

            readonly Fragment[] _fragments =
            {
                PricesListFragment,
                BlotterFragment,
                StatusFragment
            };

            public TabsAdapter(FragmentManager fm) : base(fm)
            {
            }

            public override int Count => _fragments.Length;

            public override Fragment GetItem(int position)
            {
                return _fragments[position];
            }

            public override ICharSequence GetPageTitleFormatted(int position)
            {
                return new String(_tabName[position]);
            }
        }
    }
}