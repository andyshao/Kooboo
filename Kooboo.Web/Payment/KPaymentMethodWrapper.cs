﻿using Kooboo.Data.Context;
using Kooboo.Data.Models;
using Kooboo.IndexedDB.Dynamic;
using Kooboo.Sites.Extensions;
using Kooboo.Web.Payment.Models;
using System;
using System.Collections.Generic;
using System.Text;
using Kooboo.Web.Payment.Repository;
using System.ComponentModel;

namespace Kooboo.Web.Payment
{
    public class KPaymentMethodWrapper
    {
        public KPaymentMethodWrapper(IPaymentMethod paymentMethod, RenderContext context)
        {
            this.PaymentMethod = paymentMethod;
            this.Context = context;
        }

        private RenderContext Context { get; set; }

        private IPaymentMethod PaymentMethod { get; set; }

        public IPaymentResponse Charge(object value)
        {
            var request = ParseRequest(value);

            PaymentManager.ValidateRequest(this.PaymentMethod, request, this.Context);

            var sitedb = this.Context.WebSite.SiteDb();

            var repo = sitedb.GetSiteRepository<Kooboo.Web.Payment.Repository.TempPaymentRequestRepository>();
            repo.AddOrUpdate(request);
            var result = this.PaymentMethod.Charge(request);
            if (!string.IsNullOrWhiteSpace(request.Code) || !string.IsNullOrWhiteSpace(request.ReferenceId))
            {
                repo.AddOrUpdate(request);
            }
            return result;
        }

        public PaymentStatusResponse checkStatus(object requestId)
        {
            if (requestId == null)
            {
                string strid = requestId.ToString();
                Guid id;
                if (System.Guid.TryParse(strid, out id))
                {
                    var request = Kooboo.Web.Payment.PaymentManager.GetRequest(id, Context);

                    if (request != null)
                    {
                        return this.PaymentMethod.checkStatus(request);
                    }
                }
            }

            return new PaymentStatusResponse();
        }

        internal PaymentRequest ParseRequest(object dataobj)
        {
            Dictionary<string, object> additionals = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            PaymentRequest request = new PaymentRequest();

            System.Collections.IDictionary idict = dataobj as System.Collections.IDictionary;

            IDictionary<string, object> dynamicobj = null;

            if (idict == null)
            {
                dynamicobj = dataobj as IDictionary<string, object>;
                foreach (var item in dynamicobj)
                {
                    additionals[item.Key] = item.Value;
                }
            }
            else
            {
                foreach (var item in idict.Keys)
                {
                    if (item != null)
                    {
                        additionals[item.ToString()] = idict[item];
                    }
                }
            }

            request.Additional = additionals;


            var id = GetValue<string>(idict, dynamicobj, "id", "requestId", "paymentrequestid");
            if (!string.IsNullOrWhiteSpace(id))
            {
                if (Guid.TryParse(id, out Guid requestid))
                {
                    request.Id = requestid;
                }
            }

            request.Name = GetValue<string>(idict, dynamicobj, "name", "title");
            request.Description = GetValue<string>(idict, dynamicobj, "des", "description", "detail");
            request.Currency = GetValue<string>(idict, dynamicobj, "currency");
            request.TotalAmount = GetValue<Decimal>(idict, dynamicobj, "amount", "total", "totalAmount", "totalamount");

            if (this.PaymentMethod != null)
            {
                request.PaymentMethod = PaymentMethod.Name;
            }

            request.OrderId = GetValue<Guid>(idict, dynamicobj, "orderId", "orderid");
            if (request.OrderId == default(Guid))
            {
                request.Order = GetValue<string>(idict, dynamicobj, "order", "orderId");
            }

            request.Code = GetValue<string>(idict, dynamicobj, "code");

            request.ReferenceId = GetValue<string>(idict, dynamicobj, "ref", "reference");

            return request;
        }

        private T GetValue<T>(System.Collections.IDictionary idict, IDictionary<string, object> Dynamic, params string[] fieldnames)
        {
            var type = typeof(T);

            object Value = null;

            foreach (var item in fieldnames)
            {
                if (idict != null)
                {
                    Value = Accessor.GetValueIDict(idict, item, type);
                }
                else if (Dynamic != null)
                {
                    Value = Accessor.GetValue(Dynamic, item, type);
                }

                if (Value != null)
                {
                    break;
                }
            }

            if (Value != null)
            {
                return (T)Value;
            }

            return default(T);
        }

    }




}
