module SessionsHelper
  def log_in(user)
    session[:user_id] = user.id
    session[:login_id] = user.login_id
    session[:teacher_flag] = user.teacher_flag
    session[:admin_flag] = user.admin_flag

    cookies.signed[:user_id] = {
      :value => user.id,
      :expires => 30.day.from_now,
    }
  end

  def current_user
    @current_user ||= User.find(cookies.signed[:user_id])
  end

  def logged_in?
    !current_user.nil?
  end
end
