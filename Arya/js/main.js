var __indexOf = [].indexOf || function(item) { for (var i = 0, l = this.length; i < l; i++) { if (i in this && this[i] === item) return i; } return -1; },
  __slice = [].slice;

window.Unep = {};

Unep.Pubs = (function() {
  var convertTimestampToDate, errorHandler, formatDate, formatTimestamp, getConventionsFilter, getFilter, getMultiListPropertyFilters, getPropertieFilter, getProperty, getUrlParameterByName, init, initBackToTop, initChemicals, initConventions, initLanguages, initLifeCycles, initList, initMvvm, initProperties, initTerms, listBinding, listBounds, onLoadDoc, updateList, viewModel, _conventionsMapping, _debug, _defaultFilter, _elList, _elPager, _elTerms, _filesTemplate, _langDefault, _languagesByName, _languagesMapping, _lifeCycles, _monthNames, _odata3schema, _params, _properties, _sourceTerm;
  _elList = null;
  _elTerms = null;
  _elPager = null;
  _params = {};
  _properties = {};
  _lifeCycles = [];
  _languagesByName = {};
  _langDefault = null;
  _sourceTerm = null;
  _debug = false;
  _defaultFilter = {
    language: "English",
    sourceTerm: "Scientific and Technical"
  };
  _languagesMapping = [
    {
      name: "English",
      title: "English"
    }, {
      name: "French",
      title: "Français"
    }, {
      name: "Spanish",
      title: "Español"
    }, {
      name: "Russian",
      title: "Русский"
    }, {
      name: "Arabic",
      title: "العربية"
    }, {
      name: "Chinese",
      title: "中国的"
    }
  ];
  _conventionsMapping = {
    "1a461182-6564-4e22-900b-9e06bcc90208": {
      name: "Basel",
      download: "http://www.basel.int/Portals/4/download.aspx?d="
    },
    "46dfc561-4d8c-4fe9-ab0e-1ca56ebe2c40": {
      name: "Rotterdam",
      download: "http://www.pic.int/Portals/5/download.aspx?d="
    },
    "1cfa1310-5483-49c1-86d5-c46856086dcd": {
      name: "Stockholm",
      download: "http://chm.pops.int/Portals/0/download.aspx?d="
    }
  };
  _odata3schema = {
    data: function(data) {
      return data.value;
    },
    total: function(data) {
      return data["odata.count"];
    }
  };
  init = function(params) {
    var termFromUrl;
    console.debug = console.log;
    _debug = getUrlParameterByName("debug") === "true" ? true : false;
    termFromUrl = getUrlParameterByName("term");
    if (termFromUrl) {
      _defaultFilter.sourceTerm = termFromUrl;
    }
    _params = params;
    console.debug("Init Unep.Pubs application");
    console.debug("Service url " + params.serviceUrl);
    async.parallel([
      initConventions, initLanguages, function(callback) {
        return async.series([initLifeCycles, initTerms, initChemicals], function() {
          return callback();
        });
      }
    ], function() {
      initMvvm();
      return initList();
    });
    initBackToTop();
    return true;
  };
  viewModel = kendo.observable({
    terms: null,
    cycles: null,
    chemicals: null,
    conventions: []
  });
  initMvvm = function() {
    console.debug("Init MVVM");
    kendo.bind($("#arya-filters"), viewModel);
    return viewModel.bind("change", updateList);
  };
  initProperties = function(callback) {
    var dataSource;
    console.debug("Init properties");
    dataSource = new kendo.data.DataSource({
      type: "odata",
      transport: {
        read: {
          url: "" + _params.serviceUrl + "/Properties"
        }
      },
      schema: _odata3schema,
      serverSorting: true,
      sort: {
        field: "Name"
      }
    });
    dataSource.fetch(function() {
      _properties = this.data();
      return console.debug("Number of properties " + _properties.length);
    });
    return true;
  };
  initLifeCycles = function(callback) {
    console.debug("Init Life Cycles");
    $.ajax({
      url: "http://informea.pops.int/Meetings2/asbMeetings.svc/Terms",
      data: {
        $filter: "substringof('Life Cycle Steps',ParentTermNames)",
        $format: "json"
      },
      dataType: "jsonp",
      jsonp: "$callback",
      error: function(e) {
        return callback(e);
      },
      success: function(data) {
        var item, _i, _len, _ref;
        _ref = data.d.results;
        for (_i = 0, _len = _ref.length; _i < _len; _i++) {
          item = _ref[_i];
          _lifeCycles.push(item.Name);
        }
        return callback(null);
      }
    });
    return true;
  };
  initTerms = function(callback) {
    var cyclesDataSource, dataSource, _elCycles;
    console.debug("Init terms");
    dataSource = new kendo.data.DataSource({
      type: "odata",
      transport: {
        read: {
          url: "" + _params.serviceUrl + "/PropValues",
          data: {
            $expand: "Property"
          }
        }
      },
      serverSorting: true,
      sort: {
        field: "Value",
        dir: "asc"
      },
      serverFiltering: true,
      filter: {
        field: "Property/Name",
        operator: "eq",
        value: "Term"
      },
      schema: _odata3schema
    });
    _elTerms = $("#arya-terms");
    _elTerms.attr("data-bind", "value: terms");
    _elCycles = $("#arya-cycles");
    _elCycles.attr("data-bind", "value: cycles");
    cyclesDataSource = [];
    _elTerms.kendoMultiSelect({
      dataSource: dataSource,
      dataTextField: "Value",
      dataValueField: "PropertyValueId",
      autoBind: false
    });
    return dataSource.fetch(function() {
      var data, formTerms, item, _i, _len, _ref;
      data = this.data();
      formTerms = [];
      cyclesDataSource = [];
      for (_i = 0, _len = data.length; _i < _len; _i++) {
        item = data[_i];
        if (_ref = item.Value, __indexOf.call(_lifeCycles, _ref) >= 0) {
          cyclesDataSource.push(item);
        } else {
          if (item.Value !== _defaultFilter.sourceTerm) {
            formTerms.push(item);
          } else {
            _sourceTerm = item.PropertyValueId;
          }
        }
      }
      this.data(formTerms);
      return _elCycles.kendoMultiSelect({
        dataSource: cyclesDataSource,
        dataTextField: "Value",
        dataValueField: "PropertyValueId",
        dataBound: function() {
          return callback(null, true);
        }
      });
    });
  };
  errorHandler = function(e) {
    $("#arya-error").show();
    return $("#arya-error").append("<p>" + e.status + " " + e.errorThrown + "</p>");
  };
  initChemicals = function(callback) {
    var dataSource, _elChemicals;
    console.debug("Init chemicals");
    dataSource = new kendo.data.DataSource({
      type: "odata",
      transport: {
        read: {
          url: "" + _params.serviceUrl + "/PropValues?$filter=Property/Name eq 'Chemical' and Documents/any(d:d/Properties/any(p:p/PropertyValueId eq " + _sourceTerm + "))",
          data: {
            $expand: "Property,Documents,Documents/Properties"
          }
        }
      },
      serverSorting: true,
      sort: {
        field: "Value",
        dir: "asc"
      },
      serverFiltering: true,
      schema: _odata3schema
    });
    dataSource.bind("error", errorHandler);
    _elChemicals = $("#arya-chemicals");
    _elChemicals.attr("data-bind", "value: chemicals");
    return _elChemicals.kendoMultiSelect({
      dataSource: dataSource,
      dataTextField: "Value",
      dataValueField: "PropertyValueId",
      dataBound: function() {
        return callback();
      }
    });
  };
  initConventions = function(callback) {
    var dataSource, _elRepositories;
    console.debug("Init conventions");
    dataSource = new kendo.data.DataSource({
      type: "odata",
      transport: {
        read: {
          url: "" + _params.serviceUrl + "/Repositories"
        }
      },
      serverSorting: true,
      sort: {
        field: "Name",
        dir: "asc"
      },
      schema: _odata3schema,
      serverFiltering: true
    });
    dataSource.bind("error", errorHandler);
    _elRepositories = $("#arya-repositories");
    return _elRepositories.kendoListView({
      dataSource: dataSource,
      template: $("#arya-repositorie-template").html(),
      dataBinding: function(e) {
        var item, _i, _len, _ref, _results;
        _ref = e.items;
        _results = [];
        for (_i = 0, _len = _ref.length; _i < _len; _i++) {
          item = _ref[_i];
          _results.push(item["Title"] = _conventionsMapping[item.Guid].name);
        }
        return _results;
      },
      dataBound: function() {
        var data, item, _i, _len;
        data = this.dataSource.data();
        for (_i = 0, _len = data.length; _i < _len; _i++) {
          item = data[_i];
          viewModel.conventions.push(item.Guid);
        }
        $("input[name='arya-repositories']").attr("data-bind", "checked: conventions");
        return callback(null, true);
      }
    });
  };
  initLanguages = function(callback) {
    var dataSource;
    console.debug("Init languages");
    dataSource = new kendo.data.DataSource({
      type: "odata",
      transport: {
        read: {
          url: "" + _params.serviceUrl + "/PropValues"
        }
      },
      serverSorting: true,
      sort: {
        field: "Value",
        dir: "asc"
      },
      schema: _odata3schema,
      serverFiltering: true,
      filter: {
        field: "Property/Name",
        operator: "eq",
        value: "Language"
      }
    });
    dataSource.bind("error", errorHandler);
    dataSource.fetch(function() {
      var data, lang, _i, _len;
      data = this.data();
      for (_i = 0, _len = data.length; _i < _len; _i++) {
        lang = data[_i];
        _languagesByName[lang.Value] = lang.PropertyValueId;
        if (lang.Value === _defaultFilter.language) {
          _langDefault = lang.PropertyValueId;
        }
      }
      return callback(null, true);
    });
    return true;
  };
  initList = function() {
    var dataSource;
    console.debug("Init list");
    dataSource = new kendo.data.DataSource({
      type: "odata",
      transport: {
        read: {
          url: "" + _params.serviceUrl + "/Documents",
          data: {
            $filter: null,
            $expand: "Repository,Files,Properties,Properties/Property"
          }
        }
      },
      serverSorting: true,
      sort: {
        field: "PubDate",
        dir: "desc"
      },
      serverPaging: true,
      serverFiltering: true,
      pageSize: 10,
      schema: _odata3schema
    });
    dataSource.bind("error", errorHandler);
    _elList = $("#arya-list");
    _elList.kendoListView({
      dataSource: dataSource,
      template: $("#arya-list-template").html(),
      autoBind: false
    });
    _elList.data("kendoListView").bind("dataBinding", listBinding);
    _elList.data("kendoListView").bind("dataBound", listBounds);
    _elPager = $("#arya-pager");
    _elPager.kendoPager({
      dataSource: dataSource
    });
    return updateList();
  };
  getProperty = function(name, list, d) {
    var item, _i, _len;
    for (_i = 0, _len = list.length; _i < _len; _i++) {
      item = list[_i];
      if (item.Property.Name === name) {
        return item.Value;
      }
    }
    return "";
  };
  convertTimestampToDate = function(ts) {
    var date;
    return date = new Date(ts * 1000);
  };
  _monthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
  formatDate = function(date) {
    if (date.getFullYear() !== 1970) {
      return "" + _monthNames[date.getMonth()] + " " + (date.getFullYear());
    } else {
      return "";
    }
  };
  formatTimestamp = function(ts) {
    return formatDate(convertTimestampToDate(ts));
  };
  listBinding = function(e) {
    var i, item, _i, _len, _ref;
    _ref = e.items;
    for (i = _i = 0, _len = _ref.length; _i < _len; i = ++_i) {
      item = _ref[i];
      item.Title = getProperty("Title", item.Properties, "No Title");
      item.UNNumber = getProperty("UNNumber", item.Properties);
      item.PubDateFormatted = formatTimestamp(item.PubDate);
      if (_debug) {
        item.Description = getProperty("Description", item.Properties, " ");
      } else {
        item.Description = "";
      }
    }
    return true;
  };
  listBounds = function() {
    return $(".arya-row", _elList).each(function() {
      return onLoadDoc(this);
    });
  };
  getPropertieFilter = function(id) {
    return "Properties/any(p:p/PropertyValueId eq " + id + ")";
  };
  getMultiListPropertyFilters = function() {
    var l, list, p, ret, _i, _j, _len, _len1;
    list = 1 <= arguments.length ? __slice.call(arguments, 0) : [];
    ret = [];
    for (_i = 0, _len = list.length; _i < _len; _i++) {
      l = list[_i];
      if (l) {
        for (_j = 0, _len1 = l.length; _j < _len1; _j++) {
          p = l[_j];
          ret.push(getPropertieFilter(p.PropertyValueId));
        }
      }
    }
    return ret;
  };
  getConventionsFilter = function(list) {
    var l, ret, s, _i, _len;
    s = [];
    for (_i = 0, _len = list.length; _i < _len; _i++) {
      l = list[_i];
      s.push("Repository/Guid eq guid'" + l + "'");
    }
    ret = getFilter("or", s);
    if (ret !== "()") {
      return "" + ret;
    }
    return "(1 eq 2)";
  };
  getFilter = function(logic, filters) {
    var filter, ret, sep, _i, _len;
    sep = "";
    ret = "";
    for (_i = 0, _len = filters.length; _i < _len; _i++) {
      filter = filters[_i];
      ret += "" + sep + filter;
      sep = " " + logic + " ";
    }
    return "(" + ret + ")";
  };
  updateList = function() {
    var filter, filters;
    console.debug("Update list");
    filters = ["Files/any(f:f/Extension eq 'pdf')", getPropertieFilter(_langDefault), getConventionsFilter(viewModel.conventions)].concat(getMultiListPropertyFilters(viewModel.terms), getMultiListPropertyFilters(viewModel.chemicals), getMultiListPropertyFilters(viewModel.cycles));
    if (_sourceTerm !== null) {
      filters.push("Properties/any(p:p/PropertyValueId eq " + _sourceTerm + ")");
    }
    filter = getFilter("and", filters);
    _elList.data("kendoListView").dataSource.transport.options.read.data.$filter = filter;
    _elPager.data("kendoPager").dataSource.page(1);
    return true;
  };
  _filesTemplate = kendo.template($("#arya-files-template").html());
  onLoadDoc = function(e) {
    var baseUrl, files;
    files = $(".arya-row-files", e);
    baseUrl = _conventionsMapping[$(e).data("convention")].download;
    return $.ajax({
      url: "" + _params.serviceUrl + "/Documents",
      data: {
        $expand: "Properties,Properties/Property,Files",
        $filter: ("Properties/any(p:p/Value eq '" + ($(e).data("unnumber")) + "')") + " and Files/any(f:f/Extension eq 'pdf')",
        $format: "json"
      },
      dataType: "jsonp",
      jsonp: "$callback",
      success: function(data) {
        var html, item, l, links, pv, _i, _j, _k, _len, _len1, _len2, _ref, _ref1;
        links = [];
        for (_i = 0, _len = _languagesMapping.length; _i < _len; _i++) {
          l = _languagesMapping[_i];
          _ref = data.value;
          for (_j = 0, _len1 = _ref.length; _j < _len1; _j++) {
            item = _ref[_j];
            _ref1 = item.Properties;
            for (_k = 0, _len2 = _ref1.length; _k < _len2; _k++) {
              pv = _ref1[_k];
              if (l.name === pv.Value) {
                links.push({
                  Lang: l.title,
                  Url: "" + baseUrl + item.NameOrTitle + ".pdf"
                });
              }
            }
          }
        }
        html = _filesTemplate(links);
        return files.html(html);
      }
    });
  };
  initBackToTop = function() {
    var duration, offset;
    offset = 220;
    duration = 500;
    jQuery(window).scroll(function() {
      if (jQuery(this).scrollTop() > offset) {
        return jQuery('.back-to-top').fadeIn(duration);
      } else {
        return jQuery('.back-to-top').fadeOut(duration);
      }
    });
    return jQuery('.back-to-top').click(function(event) {
      event.preventDefault();
      jQuery('html, body').animate({
        scrollTop: 0
      }, duration);
      return false;
    });
  };
  getUrlParameterByName = function(name) {
    var regex, results;
    name = name.replace(/[\[]/, "\\[").replace(/[\]]/, "\\]");
    regex = new RegExp("[\\?&]" + name + "=([^&#]*)");
    results = regex.exec(location.search);
    if (results === null) {
      return "";
    } else {
      return decodeURIComponent(results[1].replace(/\+/g, " "));
    }
  };
  return {
    init: init,
    onLoadDoc: onLoadDoc
  };
})();

$(function() {
  return Unep.Pubs.init({
    serviceUrl: "http://informea.pops.int/mfilesDocs3/Publications.svc"
  });
});
