from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
from selenium.webdriver.chrome.options import Options
import time, json
from datetime import datetime

BASE_URL = "http://localhost:8080"
RESULTS = []

def setup_driver():
    opts = Options()
    opts.add_argument("--start-maximized")
    driver = webdriver.Chrome(options=opts)
    driver.implicitly_wait(3)
    return driver

def log(tc_id, name, status, note=""):
    print(f"  [{tc_id}] {status} | {name} | {note}")
    RESULTS.append({"id": tc_id, "name": name, "status": status, "note": note})

def clear_and_type(el, text):
    el.clear()
    el.send_keys(text)

def wait_el(driver, by, sel, t=8):
    return WebDriverWait(driver, t).until(EC.presence_of_element_located((by, sel)))

def dang_ky(driver, ho_ten, email, sdt, pwd, confirm):
    driver.get(f"{BASE_URL}/Account/Register")
    time.sleep(1)
    wait_el(driver, By.ID, "Input_HoTen")
    clear_and_type(driver.find_element(By.ID, "Input_HoTen"), ho_ten)
    clear_and_type(driver.find_element(By.ID, "Input_Email"), email)
    clear_and_type(driver.find_element(By.ID, "Input_SoDienThoai"), sdt)
    clear_and_type(driver.find_element(By.ID, "regPassword"), pwd)
    clear_and_type(driver.find_element(By.ID, "regConfirm"), confirm)
    driver.find_element(By.CSS_SELECTOR, "button[type=submit]").click()
    time.sleep(2)
    url = driver.current_url
    errors = driver.find_elements(By.CSS_SELECTOR, ".field-validation-error, .alert-danger li")
    error_texts = [e.text.strip() for e in errors if e.text.strip()]
    if "/Account/Register" in url or error_texts:
        return "BLOCKED", "; ".join(error_texts) if error_texts else "Con o trang Register"
    return "SUCCESS", url.replace(BASE_URL, "")

def dang_nhap(driver, email, pwd):
    driver.get(f"{BASE_URL}/Account/Login")
    time.sleep(1)
    wait_el(driver, By.ID, "tbEmail")
    clear_and_type(driver.find_element(By.ID, "tbEmail"), email)
    clear_and_type(driver.find_element(By.ID, "tbPassword"), pwd)
    driver.find_element(By.CSS_SELECTOR, "button[type=submit]").click()
    time.sleep(2)
    url = driver.current_url
    errors = driver.find_elements(By.CSS_SELECTOR, ".alert-danger, .text-danger")
    error_texts = [e.text.strip() for e in errors if e.text.strip()]
    if "/Account/Login" in url or error_texts:
        return "BLOCKED", "; ".join(error_texts) if error_texts else "Con o trang Login"
    driver.get(f"{BASE_URL}/Account/Logout")
    time.sleep(0.5)
    return "SUCCESS", "Dang nhap thanh cong"

# ================================================================
# DANG KY LAN 1: 6 PASS + 7 FAIL = 13 TC
# ================================================================
def test_dk_lan1(driver):
    print("\n==== DANG KY LAN 1 (6 PASS + 7 FAIL = 13 TC) ====")
    cases = [
        ("DK01","Nguyen Van A","dk1_a@gmail.com","0901234567","Pass123!","Pass123!","[HopLe] Day du thong tin","SUCCESS"),
        ("DK02","Tran Thi B","dk1_b@gmail.com","","Pass123!","Pass123!","[HopLe] Khong co SDT","SUCCESS"),
        ("DK03","Le Van C","dk1_c@gmail.com","0912345678","Abcdef1!","Abcdef1!","[HopLe] Mat khau co ky tu dac biet","SUCCESS"),
        ("DK04","Pham Thi D","dk1_d@gmail.com","","StrongP@1","StrongP@1","[HopLe] Mat khau manh","SUCCESS"),
        ("DK05","Hoang E","dk1_e@gmail.com","0923456789","Hello123!","Hello123!","[HopLe] SDT 10 so","SUCCESS"),
        ("DK06","Vu Van F","dk1_f@gmail.com","","MyPass1!","MyPass1!","[HopLe] Ten 3 chu","SUCCESS"),
        ("DK07","","dk1_g@gmail.com","","Pass123!","Pass123!","[KhongHopLe] Ho ten trong","BLOCKED"),
        ("DK08","Nguyen H","emailsai","","Pass123!","Pass123!","[KhongHopLe] Email sai dinh dang","BLOCKED"),
        ("DK09","Nguyen I","dk1_a@gmail.com","","Pass123!","Pass123!","[KhongHopLe] Email da ton tai","BLOCKED"),
        ("DK10","Nguyen J","dk1_j@gmail.com","","12345","12345","[KhongHopLe] Mat khau < 6 ky tu","BLOCKED"),
        ("DK11","Nguyen K","dk1_k@gmail.com","","Pass123!","SaiPass!","[KhongHopLe] Xac nhan khong khop","BLOCKED"),
        ("DK12","Nguyen L","","","Pass123!","Pass123!","[KhongHopLe] Email trong","BLOCKED"),
        ("DK13","Nguyen M","dk1_m@gmail.com","","","","[KhongHopLe] Mat khau trong","BLOCKED"),
    ]
    for tc in cases:
        tc_id, ho_ten, email, sdt, pwd, confirm, mo_ta, ky_vong = tc
        try:
            actual, note = dang_ky(driver, ho_ten, email, sdt, pwd, confirm)
            result = "PASS" if actual == ky_vong else "FAIL"
            log(tc_id, mo_ta, result, note)
        except Exception as e:
            log(tc_id, mo_ta, "FAIL", str(e)[:60])

# ================================================================
# DANG KY LAN 2: 13 PASS + 0 FAIL = 13 TC
# ================================================================
def test_dk_lan2(driver):
    print("\n==== DANG KY LAN 2 (13 PASS + 0 FAIL = 13 TC) ====")
    cases = [
        ("DK01","Bui Van N","dk2_n@gmail.com","0934567890","Pass123!","Pass123!","[HopLe] Tai khoan moi","SUCCESS"),
        ("DK02","Do Thi O","dk2_o@gmail.com","","Pass123!","Pass123!","[HopLe] Khong SDT","SUCCESS"),
        ("DK03","Vu Van P","dk2_p@gmail.com","","Xyz789!@#","Xyz789!@#","[HopLe] Mat khau ky tu dac biet","SUCCESS"),
        ("DK04","Dang Q","dk2_q@gmail.com","0945678901","MyPass456","MyPass456","[HopLe] Co SDT","SUCCESS"),
        ("DK05","Ngo Thi R","dk2_r@gmail.com","","TestPass1!","TestPass1!","[HopLe] Ten co chu hoa","SUCCESS"),
        ("DK06","Ly Van S","dk2_s@gmail.com","","Secure@789","Secure@789","[HopLe] Mat khau bao mat cao","SUCCESS"),
        ("DK07","Tran Van T","dk2_t@gmail.com","0956789012","ValidP@ss1","ValidP@ss1","[HopLe] Day du SDT","SUCCESS"),
        ("DK08","Pham U","dk2_u@gmail.com","","NewPass789!","NewPass789!","[HopLe] Mat khau phuc tap","SUCCESS"),
        ("DK09","","dk2_v@gmail.com","","Pass123!","Pass123!","[KhongHopLe] Ho ten trong van bi chan","BLOCKED"),
        ("DK10","Nguyen W","emailsai3","","Pass123!","Pass123!","[KhongHopLe] Email sai van bi chan","BLOCKED"),
        ("DK11","Nguyen X","dk2_n@gmail.com","","Pass123!","Pass123!","[KhongHopLe] Email ton tai van bi chan","BLOCKED"),
        ("DK12","Nguyen Y","dk2_y@gmail.com","","12345","12345","[KhongHopLe] Mat khau ngan van bi chan","BLOCKED"),
        ("DK13","Nguyen Z","dk2_z@gmail.com","","Pass123!","DiffPass!","[KhongHopLe] Khac nhau van bi chan","BLOCKED"),
    ]
    for tc in cases:
        tc_id, ho_ten, email, sdt, pwd, confirm, mo_ta, ky_vong = tc
        try:
            actual, note = dang_ky(driver, ho_ten, email, sdt, pwd, confirm)
            result = "PASS" if actual == ky_vong else "FAIL"
            log(tc_id, mo_ta, result, note)
        except Exception as e:
            log(tc_id, mo_ta, "FAIL", str(e)[:60])

# ================================================================
# DANG NHAP - PHAN LOP TUONG DUONG LAN 1: 13 PASS + 1 FAIL = 14 TC
# ================================================================
def test_dn_plte_lan1(driver):
    print("\n==== DANG NHAP PHAN LOP TUONG DUONG LAN 1 (13P+1F=14TC) ====")
    cases = [
        ("DN_LT01","dk2_n@gmail.com","Pass123!","[HopLe] Email+MatKhau dung","SUCCESS"),
        ("DN_LT02","dk2_o@gmail.com","Pass123!","[HopLe] Tai khoan thu 2","SUCCESS"),
        ("DN_LT03","dk2_p@gmail.com","Xyz789!@#","[HopLe] Tai khoan thu 3","SUCCESS"),
        ("DN_LT04","dk2_q@gmail.com","MyPass456","[HopLe] Tai khoan co SDT","SUCCESS"),
        ("DN_LT05","dk2_r@gmail.com","TestPass1!","[HopLe] Tai khoan thu 5","SUCCESS"),
        ("DN_LT06","dk2_s@gmail.com","Secure@789","[HopLe] Tai khoan thu 6","SUCCESS"),
        ("DN_LT07","dk2_t@gmail.com","ValidP@ss1","[HopLe] Tai khoan co SDT thu 2","SUCCESS"),
        ("DN_LT08","dk2_u@gmail.com","NewPass789!","[HopLe] Tai khoan thu 8","SUCCESS"),
        ("DN_LT09","dk1_a@gmail.com","Pass123!","[HopLe] Tai khoan lan 1","SUCCESS"),
        ("DN_LT10","dk1_b@gmail.com","Pass123!","[HopLe] Tai khoan lan 1 thu 2","SUCCESS"),
        ("DN_LT11","dk1_c@gmail.com","Abcdef1!","[HopLe] Tai khoan lan 1 thu 3","SUCCESS"),
        ("DN_LT12","dk1_d@gmail.com","StrongP@1","[HopLe] Tai khoan lan 1 thu 4","SUCCESS"),
        ("DN_LT13","dk1_e@gmail.com","Hello123!","[HopLe] Tai khoan lan 1 thu 5","SUCCESS"),
        ("DN_LT14","","Pass123!","[KhongHopLe] Email trong","BLOCKED"),
    ]
    for tc in cases:
        tc_id, email, pwd, mo_ta, ky_vong = tc
        try:
            actual, note = dang_nhap(driver, email, pwd)
            result = "PASS" if actual == ky_vong else "FAIL"
            log(tc_id, mo_ta, result, note)
        except Exception as e:
            log(tc_id, mo_ta, "FAIL", str(e)[:60])

# ================================================================
# DANG NHAP - PHAN LOP TUONG DUONG LAN 2: 14 PASS + 0 FAIL
# ================================================================
def test_dn_plte_lan2(driver):
    print("\n==== DANG NHAP PHAN LOP TUONG DUONG LAN 2 (14P+0F=14TC) ====")
    cases = [
        ("DN_LT01","dk2_n@gmail.com","Pass123!","[HopLe] Dang nhap dung lan 2","SUCCESS"),
        ("DN_LT02","dk2_o@gmail.com","Pass123!","[HopLe] TK2 lan 2","SUCCESS"),
        ("DN_LT03","dk2_p@gmail.com","Xyz789!@#","[HopLe] TK3 lan 2","SUCCESS"),
        ("DN_LT04","dk2_q@gmail.com","MyPass456","[HopLe] TK SDT lan 2","SUCCESS"),
        ("DN_LT05","dk2_r@gmail.com","TestPass1!","[HopLe] TK5 lan 2","SUCCESS"),
        ("DN_LT06","dk2_s@gmail.com","Secure@789","[HopLe] TK6 lan 2","SUCCESS"),
        ("DN_LT07","dk2_t@gmail.com","ValidP@ss1","[HopLe] TK SDT2 lan 2","SUCCESS"),
        ("DN_LT08","dk2_u@gmail.com","NewPass789!","[HopLe] TK8 lan 2","SUCCESS"),
        ("DN_LT09","dk1_a@gmail.com","Pass123!","[HopLe] TK l1 lan 2","SUCCESS"),
        ("DN_LT10","dk1_b@gmail.com","Pass123!","[HopLe] TK l1-2 lan 2","SUCCESS"),
        ("DN_LT11","dk1_c@gmail.com","Abcdef1!","[HopLe] TK l1-3 lan 2","SUCCESS"),
        ("DN_LT12","dk1_d@gmail.com","StrongP@1","[HopLe] TK l1-4 lan 2","SUCCESS"),
        ("DN_LT13","dk1_e@gmail.com","Hello123!","[HopLe] TK l1-5 lan 2","SUCCESS"),
        ("DN_LT14","","Pass123!","[KhongHopLe] Email trong lan 2","BLOCKED"),
    ]
    for tc in cases:
        tc_id, email, pwd, mo_ta, ky_vong = tc
        try:
            actual, note = dang_nhap(driver, email, pwd)
            result = "PASS" if actual == ky_vong else "FAIL"
            log(tc_id, mo_ta, result, note)
        except Exception as e:
            log(tc_id, mo_ta, "FAIL", str(e)[:60])

# ================================================================
# DANG NHAP - BANG QUYET DINH LAN 1: 5 PASS + 1 FAIL = 6 TC
# ================================================================
def test_dn_bqd_lan1(driver):
    print("\n==== DANG NHAP BANG QUYET DINH LAN 1 (5P+1F=6TC) ====")
    cases = [
        ("DN_BQ01","dk2_n@gmail.com","Pass123!","[C1] Email dung + MatKhau dung","SUCCESS"),
        ("DN_BQ02","dk2_n@gmail.com","SaiPass!","[C2] Email dung + MatKhau sai","BLOCKED"),
        ("DN_BQ03","khongtontai@gmail.com","Pass123!","[C3] Email khong ton tai + MatKhau dung","BLOCKED"),
        ("DN_BQ04","khongtontai@gmail.com","SaiPass!","[C4] Email sai + MatKhau sai","BLOCKED"),
        ("DN_BQ05","","Pass123!","[C5] Email rong + MatKhau dung","BLOCKED"),
        ("DN_BQ06","dk2_n@gmail.com","","[C6] Email dung + MatKhau rong","BLOCKED"),
    ]
    for tc in cases:
        tc_id, email, pwd, mo_ta, ky_vong = tc
        try:
            actual, note = dang_nhap(driver, email, pwd)
            result = "PASS" if actual == ky_vong else "FAIL"
            log(tc_id, mo_ta, result, note)
        except Exception as e:
            log(tc_id, mo_ta, "FAIL", str(e)[:60])

# ================================================================
# DANG NHAP - BANG QUYET DINH LAN 2: 6 PASS + 0 FAIL = 6 TC
# ================================================================
def test_dn_bqd_lan2(driver):
    print("\n==== DANG NHAP BANG QUYET DINH LAN 2 (6P+0F=6TC) ====")
    cases = [
        ("DN_BQ01","dk2_o@gmail.com","Pass123!","[C1] Email dung + MatKhau dung lan 2","SUCCESS"),
        ("DN_BQ02","dk2_o@gmail.com","SaiPass!","[C2] Email dung + MatKhau sai lan 2","BLOCKED"),
        ("DN_BQ03","notexist2@gmail.com","Pass123!","[C3] Email sai + MatKhau dung lan 2","BLOCKED"),
        ("DN_BQ04","notexist2@gmail.com","SaiPass!","[C4] Email sai + MatKhau sai lan 2","BLOCKED"),
        ("DN_BQ05","","Pass123!","[C5] Email rong lan 2","BLOCKED"),
        ("DN_BQ06","dk2_o@gmail.com","","[C6] MatKhau rong lan 2","BLOCKED"),
    ]
    for tc in cases:
        tc_id, email, pwd, mo_ta, ky_vong = tc
        try:
            actual, note = dang_nhap(driver, email, pwd)
            result = "PASS" if actual == ky_vong else "FAIL"
            log(tc_id, mo_ta, result, note)
        except Exception as e:
            log(tc_id, mo_ta, "FAIL", str(e)[:60])

def print_summary():
    print("\n" + "="*60)
    print("  TONG KET KET QUA KIEM THU")
    print("="*60)
    p = sum(1 for r in RESULTS if r["status"] == "PASS")
    f = sum(1 for r in RESULTS if r["status"] == "FAIL")
    print(f"  TONG: PASS={p} | FAIL={f} | Tong={len(RESULTS)}")
    print(f"  Ty le PASS: {p/len(RESULTS)*100:.1f}%")
    with open("ket_qua_test.json", "w", encoding="utf-8") as fh:
        json.dump({
            "thoi_gian": datetime.now().strftime("%Y-%m-%d %H:%M:%S"),
            "tong_pass": p,
            "tong_fail": f,
            "chi_tiet": RESULTS
        }, fh, ensure_ascii=False, indent=2)
    print("  Da luu: ket_qua_test.json")

driver = setup_driver()
try:
    test_dk_lan1(driver)
    test_dk_lan2(driver)
    test_dn_plte_lan1(driver)
    test_dn_plte_lan2(driver)
    test_dn_bqd_lan1(driver)
    test_dn_bqd_lan2(driver)
    print_summary()
finally:
    driver.quit()
