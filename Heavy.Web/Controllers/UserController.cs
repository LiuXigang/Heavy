using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Heavy.Web.Data;
using Heavy.Web.Models;
using Heavy.Web.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Heavy.Web.Controllers
{
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        public IActionResult AddUser()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> AddUser(UserAddViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);
            var user = new ApplicationUser
            {
                UserName = model.UserName.Trim(),
                Email = model.Email.Trim(),
                IdCardNo = model.IdCardNo.Trim(),
                BirthDate = model.BirthDate
            };
            var result = await _userManager.CreateAsync(user, model.Password.Trim());
            if (result.Succeeded)
                return RedirectToAction(nameof(Index));
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        public async Task<IActionResult> EditUser(string id)
        {
            var model = await _userManager.FindByIdAsync(id);
            var claims = await _userManager.GetClaimsAsync(model);
            var claimType = claims.Select(n => n.Type).ToList();
            ViewBag.Claims = claimType;
            return View(model);
        }
        [HttpPost]
        public async Task<IActionResult> EditUser(ApplicationUser model)
        {
            if (!ModelState.IsValid)
                return View(model);
            if (string.IsNullOrEmpty(model.Id))
            {
                ModelState.AddModelError(string.Empty, "参数无效");
                return View(model);
            }
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return RedirectToAction(nameof(Index));
            }
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.IdCardNo = model.IdCardNo;
            user.BirthDate = model.BirthDate;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, "更新用户信息时发生错误");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }
                ModelState.AddModelError(string.Empty, "删除用户发生错误");
            }
            ModelState.AddModelError(string.Empty, "找不到该用户");
            return View(nameof(Index));
        }

        public async Task<IActionResult> ManageClaims(string id)
        {
            var user = await _userManager.Users.Include(n => n.Claims).Where(x => x.Id == id).FirstOrDefaultAsync();
            if (user == null)
                return RedirectToAction(nameof(Index));
            var userClaims = user.Claims.Select(x => x.ClaimType);
            var leftClaims = ClaimTypes.AllClaimTypeList.Except(userClaims).ToList();
            var vm = new ManageClaimsViewModel
            {
                UserId = id,
                AvailableClaims = leftClaims
            };

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> ManageClaims(ManageClaimsViewModel vm)
        {
            var user = await _userManager.FindByIdAsync(vm.UserId);
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            var claim = new IdentityUserClaim<string>
            {
                ClaimType = vm.ClaimId,
                ClaimValue = vm.ClaimId
            };

            user.Claims.Add(claim);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("EditUser", new { id = vm.UserId });
            }

            ModelState.AddModelError(string.Empty, "编辑用户Claims时发生错误");
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveClaim(string id, string claim)
        {
            var user = await _userManager.Users.Include(x => x.Claims)
                .Where(x => x.Id == id).SingleOrDefaultAsync();
            if (user == null)
            {
                return RedirectToAction("Index");
            }

            var claims = user.Claims.Where(x => x.ClaimType == claim).ToList();

            foreach (var c in claims)
            {
                user.Claims.Remove(c);
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("EditUser", new { id });
            }

            ModelState.AddModelError(string.Empty, "编辑用户Claims时发生错误");
            return RedirectToAction("ManageClaims", new { id });
        }
    }
}
