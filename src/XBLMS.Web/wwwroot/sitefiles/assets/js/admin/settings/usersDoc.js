var $url = '/settings/users/doc';

var data = utils.init({
  items: null,
  count: null,
  systemCode: null,
  formInline: {
    state: '',
    groupId: utils.getQueryInt("groupId") || 0,
    order: '',
    lastActivityDate: 0,
    keyword: '',
    organId: 0,
    organType: '',
    dateFrom: '',
    dateTo: '',
    pageIndex: 1,
    pageSize: 50
  },
});

var methods = {
  apiGet: function () {
    var $this = this;

    $api.get($url, {
      params: this.formInline
    }).then(function (response) {
      var res = response.data;
      $this.systemCode = res.systemCode;
      $this.items = res.users;
      $this.count = res.count;
    }).catch(function (error) {
      utils.error(error);
    }).then(function () {
      utils.loading($this, false);
    });
  },
  btnSearchClick() {
    this.formInline.pageIndex = 1;
    this.apiGet();
  },
  handleCurrentChange: function (val) {
    this.formInline.pageIndex = val;
    this.apiGet();
  },
  handleSizeChange: function (val) {
    this.formInline.pageIndex = 1;
    this.formInline.pageSize = val;
    this.apiGet();
  },
  btnDocClick: function (id) {
    top.utils.openLayer({
      title: false,
      closebtn: 0,
      url: utils.getCommonUrl('userDocLayerView', { id: id }),
      width: "98%",
      height: "98%"
    });
  }
};

var $vue = new Vue({
  el: '#main',
  data: data,
  methods: methods,
  created: function () {
    this.apiGet();
  }
});
