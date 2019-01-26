window.onload = function () {
    const monthNames = ["January", "February", "March", "April", "May", "June",
        "July", "August", "September", "October", "November", "December"
    ];
    var d = new Date();
    // Bug fix - If current month is Jan, dispaly previous year with December
    if (d.getMonth() === 0) {
        d = monthNames[11] + "," + parseInt(d.getFullYear() - 1);
    } else {
        d = monthNames[d.getMonth() - 1] + "," + d.getFullYear();
    }
    $('#txtMonthlyDate').val(d);
    GetResult(d);
};


