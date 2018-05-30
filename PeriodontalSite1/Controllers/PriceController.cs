﻿using AutoMapper;
using PagedList;
using PeriodontalSite1.AutoMapper;
using PeriodontalSite1.Models;
using PeriodontalSite1.Repository;
using PeriodontalSite1.ViewModel;
using PeriodontalSite1.ViewModel.Price;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace PeriodontalSite1.Controllers
{
    [Authorize(Roles = "Owner")]
    public class PriceController : Controller
    {
        static ApplicationContext context = new ApplicationContext();
        private PriceService Price = new PriceService(context);
        private GenericService<Services> Services = new GenericService<Services>(context);

        public ActionResult Index(int? page, DateTime? DateTimeFilter, bool? filtrEnable)
        {
            var priceList = new List<PriceViewModel>();
            if (filtrEnable == true)
            {
                if (DateTimeFilter == null) DateTimeFilter = DateTime.Now;

                priceList = (from p in context.Prices
                              .Include("Services")
                             where (DateTimeFilter >= p.FromDate && (p.ToDate >= DateTimeFilter || p.ToDate == null))
                             select p).ToList().Map<List<PriceViewModel>>();
            }
            else
            {
                priceList = (from p in context.Prices
                              .Include("Services")
                             select p).ToList().Map<List<PriceViewModel>>();
            }

            int pageSize = 5;
            int pageNumber = (page ?? 1);
            return View(new PriceViewModel()
            {
                DateTimeFilter = DateTime.Now,
                Prices = priceList.ToPagedList(pageNumber, pageSize)
            }
            );
        }

        [HttpGet]
        public ActionResult Create()
        {
            var services = Services.Get().Select(s => new SelectListItem
            {
                Text = s.Name,
                Value = Convert.ToString(s.ServicesId)
            }).ToList();
           
            var model = new PriceCreateViewModel
            {
                Service = services
      
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult Create(PriceCreateViewModel model, string redirectUrl)
        {
            if (!ModelState.IsValid)
            {
              return View(model);
            }

            List<Prices> prices = Price.Get().Where(t => t.ServicesId == model.ServiceSelected).ToList().Where(p=>p.FromDate <= model.FromDate  &&  (p.ToDate > model.FromDate || p.ToDate == null )).ToList();
   
            if (prices.Count != 0)
            {
                prices[0].ToDate = model.FromDate;
                Price.Update(prices[0]);
            }

            var price = new Prices
            {
                ServicesId = model.ServiceSelected,
                ToDate = null,
                Value = model.Value,
                FromDate = model.FromDate
            };

            if (prices.Count == 2)
            {
                price.ToDate = prices[1].FromDate;
            }
   
            Price.Create(price);

            return RedirectToLocal(redirectUrl);
        }


        [HttpGet]
        public ActionResult Edit()
        {

            var service = (from s in context.Services
                           .Include("Types")
                           .Include("Units")
                           select (new ResultEdit { ServicesId = s.ServicesId,  Name = s.Name, Price = s.Prices.Where(p=> p.FromDate <= DateTime.Now && (p.ToDate == null || p.ToDate > DateTime.Now )).FirstOrDefault().Value})).ToList();
            PriceEditViewModel model = new PriceEditViewModel
            {
                Services = service,
                FromDate = DateTime.Now
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult Edit(PriceEditViewModel model, string redirectUrl)
        {
            if (!ModelState.IsValid)
            {
                return View(model: model);
            }

            //var serv = Price.GetById(model.PriceId);
            //if (serv != null)
            //{
            //    serv.ServicesId = model.ServiceSelected;
            //    serv.Value = model.Value;
            //    serv.FromDate = model.FromDate;
            //    Price.Update(serv);
            //}

            return RedirectToLocal(redirectUrl);
        }

        public ActionResult Delete(int id, string redirectUrl)
        {
            var result = Price.GetById(id);
            Price.Remove(result);
            return RedirectToLocal(redirectUrl);
        }


        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Index), "Price");
        }
        
    }
}